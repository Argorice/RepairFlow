using Microsoft.EntityFrameworkCore;
using RepairFlow.Api.Authorization;
using RepairFlow.Api.Common;
using RepairFlow.Api.Contracts;
using RepairFlow.Api.Data;
using RepairFlow.Api.Domain;
using RepairFlow.Api.Domain.Entities;
using RepairFlow.Api.Domain.Enums;
using RepairFlow.Api.Mapping;
using RepairFlow.Api.Realtime;

namespace RepairFlow.Api.Services;

public interface IOrderService
{
    Task<PagedResult<OrderListItemDto>> GetListAsync(OrderQuery query, CancellationToken ct = default);

    Task<OrderDetailsDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<OrderDetailsDto> CreateAsync(CreateOrderRequest request, CancellationToken ct = default);

    Task<OrderDetailsDto> UpdateAsync(Guid id, UpdateOrderRequest request, CancellationToken ct = default);

    Task<OrderDetailsDto> ChangeStatusAsync(Guid id, ChangeStatusRequest request, CancellationToken ct = default);

    Task<OrderDetailsDto> AssignAsync(Guid id, AssignTechnicianRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<OrderStatusHistoryDto>> GetHistoryAsync(Guid id, CancellationToken ct = default);
}

public sealed class OrderService : IOrderService
{
    private const int MaxPageSize = 100;

    /// <summary>Ключ advisory-блокировки Postgres для выдачи номера заявки. Значение произвольное, важна стабильность.</summary>
    private const int NumberLockKey = 4711;

    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IOrderAccessGuard _guard;
    private readonly IOrderNotifier _notifier;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        AppDbContext db,
        ICurrentUser currentUser,
        IOrderAccessGuard guard,
        IOrderNotifier notifier,
        ILogger<OrderService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _guard = guard;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<PagedResult<OrderListItemDto>> GetListAsync(OrderQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        // Ограничение по роли — первым же условием запроса, до всех пользовательских фильтров.
        var filtered = _db.Orders
            .AsNoTracking()
            .VisibleTo(_currentUser.Id, _currentUser.Role);

        if (query.Status is not null)
        {
            filtered = filtered.Where(o => o.Status == query.Status);
        }

        if (query.Priority is not null)
        {
            filtered = filtered.Where(o => o.Priority == query.Priority);
        }

        if (query.TechnicianId is not null)
        {
            filtered = filtered.Where(o => o.AssignedTechnicianId == query.TechnicianId);
        }

        if (query.ClientId is not null)
        {
            filtered = filtered.Where(o => o.ClientId == query.ClientId);
        }

        if (DateRange.ToUtc(query.From) is { } from)
        {
            filtered = filtered.Where(o => o.CreatedAt >= from);
        }

        if (DateRange.EndOfDay(query.To) is { } to)
        {
            filtered = filtered.Where(o => o.CreatedAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = "%" + EscapeLike(query.Search.Trim()) + "%";
            filtered = filtered.Where(o =>
                EF.Functions.ILike(o.Number, pattern) ||
                EF.Functions.ILike(o.Brand, pattern) ||
                EF.Functions.ILike(o.Model, pattern) ||
                EF.Functions.ILike(o.DeviceType, pattern) ||
                (o.SerialNumber != null && EF.Functions.ILike(o.SerialNumber, pattern)) ||
                EF.Functions.ILike(o.ProblemDescription, pattern));
        }

        var total = await filtered.CountAsync(ct);
        if (total == 0)
        {
            return PagedResult<OrderListItemDto>.Empty(page, pageSize);
        }

        var orders = await ApplySort(filtered, query.Sort)
            .Include(o => o.Client)
            .Include(o => o.AssignedTechnician)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<OrderListItemDto>(
            orders.Select(o => o.ToListItem()).ToList(),
            page,
            pageSize,
            total);
    }

    public async Task<OrderDetailsDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var order = await LoadAsync(id, OrderAccessRequirement.Read, tracking: false, ct);
        return await BuildDetailsAsync(order, ct);
    }

    public async Task<OrderDetailsDto> CreateAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // Транзакция нужна не ради двух вставок, а ради advisory-блокировки:
        // она держится до конца транзакции и не даёт выдать один номер двум заявкам.
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var order = new Order
        {
            Number = await NextNumberAsync(now.Year, ct),
            ClientId = _currentUser.Id,
            DeviceType = request.DeviceType.Trim(),
            Brand = request.Brand.Trim(),
            Model = request.Model.Trim(),
            SerialNumber = string.IsNullOrWhiteSpace(request.SerialNumber) ? null : request.SerialNumber.Trim(),
            ProblemDescription = request.ProblemDescription.Trim(),
            Status = OrderStatus.New,
            Priority = request.Priority ?? OrderPriority.Normal,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Orders.Add(order);
        _db.OrderStatusHistory.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = null,
            ToStatus = OrderStatus.New,
            ChangedById = _currentUser.Id,
            Comment = "Заявка создана",
            ChangedAt = now
        });

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        _logger.LogInformation("Создана заявка {Number} клиентом {UserId}", order.Number, _currentUser.Id);

        return await GetByIdAsync(order.Id, ct);
    }

    public async Task<OrderDetailsDto> UpdateAsync(Guid id, UpdateOrderRequest request, CancellationToken ct = default)
    {
        var order = await LoadAsync(id, OrderAccessRequirement.Write, tracking: true, ct);

        if (OrderStatusMachine.IsTerminal(order.Status))
        {
            throw new ConflictException("Закрытую заявку редактировать нельзя.");
        }

        if (_currentUser.IsClient && order.Status != OrderStatus.New)
        {
            throw new ForbiddenException("Заявку можно править, пока она в статусе «Новая». Дальше — через комментарий мастеру.");
        }

        if (request.DeviceType is not null)
        {
            order.DeviceType = request.DeviceType.Trim();
        }

        if (request.Brand is not null)
        {
            order.Brand = request.Brand.Trim();
        }

        if (request.Model is not null)
        {
            order.Model = request.Model.Trim();
        }

        if (request.SerialNumber is not null)
        {
            order.SerialNumber = string.IsNullOrWhiteSpace(request.SerialNumber) ? null : request.SerialNumber.Trim();
        }

        if (request.ProblemDescription is not null)
        {
            order.ProblemDescription = request.ProblemDescription.Trim();
        }

        if (request.Priority is not null)
        {
            if (_currentUser.IsClient)
            {
                throw new ForbiddenException("Приоритет заявки выставляет сервис, а не клиент.");
            }

            order.Priority = request.Priority.Value;
        }

        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await BuildDetailsAsync(order, ct);
    }

    public async Task<OrderDetailsDto> ChangeStatusAsync(Guid id, ChangeStatusRequest request, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Client)
            .Include(o => o.AssignedTechnician)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw NotFoundException.For("Заявка", id);

        await _guard.EnsureAsync(order, OrderAccessRequirement.Write);

        var check = OrderStatusMachine.Validate(_currentUser.Role, order.Status, request.Status);
        if (!check.IsAllowed)
        {
            throw new ConflictException(check.Error!);
        }

        if (OrderStatusMachine.RequiresNonEmptyEstimate(request.Status) && order.Items.Count == 0)
        {
            throw new ConflictException("Смета пустая — сначала добавьте работы или запчасти.");
        }

        var now = DateTime.UtcNow;
        var estimate = EstimateCalculator.Calculate(order.Items);
        var from = order.Status;

        // Мастер, который берёт свободную заявку в работу, автоматически становится её исполнителем.
        if (_currentUser.IsTechnician && order.AssignedTechnicianId is null)
        {
            order.AssignedTechnicianId = _currentUser.Id;
        }

        if (OrderStatusMachine.RequiresNonEmptyEstimate(request.Status))
        {
            order.EstimatedCost = estimate.Total;
        }

        if (OrderStatusMachine.IsCompletion(request.Status))
        {
            order.CompletedAt = now;
            order.FinalCost = estimate.Total;
        }

        order.Status = request.Status;
        order.UpdatedAt = now;

        _db.OrderStatusHistory.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = from,
            ToStatus = request.Status,
            ChangedById = _currentUser.Id,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            ChangedAt = now
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Заявка {Number}: {From} → {To} пользователем {UserId}",
            order.Number,
            from,
            request.Status,
            _currentUser.Id);

        // Клиент и мастер видят смену статуса сразу, без перезагрузки карточки.
        await _notifier.StatusChangedAsync(order, from, request.Comment, ct);

        return await BuildDetailsAsync(order, ct);
    }

    public async Task<OrderDetailsDto> AssignAsync(Guid id, AssignTechnicianRequest request, CancellationToken ct = default)
    {
        var order = await LoadAsync(id, OrderAccessRequirement.Manage, tracking: true, ct);

        if (OrderStatusMachine.IsTerminal(order.Status))
        {
            throw new ConflictException("Закрытую заявку назначить нельзя.");
        }

        User? technician = null;

        if (request.TechnicianId is not null)
        {
            technician = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.TechnicianId, ct)
                         ?? throw NotFoundException.For("Мастер", request.TechnicianId);

            if (technician.Role != UserRole.Technician)
            {
                throw new ConflictException("Назначать заявку можно только пользователю с ролью «Мастер».");
            }

            if (!technician.IsActive)
            {
                throw new ConflictException("Мастер отключён и не может брать заявки.");
            }
        }

        var now = DateTime.UtcNow;
        order.AssignedTechnicianId = technician?.Id;
        order.UpdatedAt = now;

        // Назначение фиксируем внутренней заметкой: клиенту она не видна, а у сотрудников остаётся след.
        _db.Comments.Add(new Comment
        {
            OrderId = order.Id,
            AuthorId = _currentUser.Id,
            IsInternal = true,
            Text = technician is null
                ? "Мастер снят с заявки."
                : $"Заявка назначена на мастера: {technician.FullName}.",
            CreatedAt = now
        });

        await _db.SaveChangesAsync(ct);

        await _notifier.TechnicianAssignedAsync(order, technician?.FullName, ct);

        return await BuildDetailsAsync(order, ct);
    }

    public async Task<IReadOnlyList<OrderStatusHistoryDto>> GetHistoryAsync(Guid id, CancellationToken ct = default)
    {
        var order = await LoadAsync(id, OrderAccessRequirement.Read, tracking: false, ct);

        var history = await _db.OrderStatusHistory
            .AsNoTracking()
            .Include(h => h.ChangedBy)
            .Where(h => h.OrderId == order.Id)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync(ct);

        return history.Select(h => h.ToDto()).ToList();
    }

    /// <summary>Загружает заявку и проверяет права на неё одним движением.</summary>
    private async Task<Order> LoadAsync(Guid id, OrderAccessRequirement requirement, bool tracking, CancellationToken ct)
    {
        var query = _db.Orders
            .Include(o => o.Client)
            .Include(o => o.AssignedTechnician)
            .AsQueryable();

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        var order = await query.FirstOrDefaultAsync(o => o.Id == id, ct)
                    ?? throw NotFoundException.For("Заявка", id);

        await _guard.EnsureAsync(order, requirement);

        return order;
    }

    private async Task<OrderDetailsDto> BuildDetailsAsync(Order order, CancellationToken ct)
    {
        // Клиент не должен видеть внутренние заметки даже в счётчике.
        var isClient = _currentUser.IsClient;

        var items = await _db.OrderItems.AsNoTracking()
            .Where(i => i.OrderId == order.Id)
            .ToListAsync(ct);

        var commentsCount = await _db.Comments.AsNoTracking()
            .CountAsync(c => c.OrderId == order.Id && (!isClient || !c.IsInternal), ct);

        var attachmentsCount = await _db.Attachments.AsNoTracking()
            .CountAsync(a => a.OrderId == order.Id, ct);

        var estimate = EstimateCalculator.Calculate(items);

        var transitions = OrderStatusMachine
            .AllowedTargetsFor(_currentUser.Role, order.Status)
            .Select(s => s.ToOption())
            .ToList();

        var canEdit = !OrderStatusMachine.IsTerminal(order.Status)
                      && (!isClient || order.Status == OrderStatus.New);

        var canManageEstimate = !isClient && OrderStatusMachine.IsEstimateEditable(order.Status);

        return new OrderDetailsDto(
            order.Id,
            order.Number,
            order.DeviceType,
            order.Brand,
            order.Model,
            order.SerialNumber,
            order.ProblemDescription,
            order.Status,
            OrderStatusMachine.Describe(order.Status),
            order.Priority,
            order.Client.ToSummary(),
            order.AssignedTechnician.ToSummaryOrNull(),
            order.EstimatedCost,
            order.FinalCost,
            estimate.Total,
            estimate.ItemCount,
            commentsCount,
            attachmentsCount,
            order.CreatedAt,
            order.UpdatedAt,
            order.CompletedAt,
            transitions,
            canEdit,
            canManageEstimate);
    }

    /// <summary>Следующий номер года под advisory-блокировкой: параллельные создания не получат одинаковый номер.</summary>
    private async Task<string> NextNumberAsync(int year, CancellationToken ct)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({NumberLockKey}, {year})",
            ct);

        var prefix = OrderNumberGenerator.YearPrefix(year);

        var last = await _db.Orders
            .Where(o => o.Number.StartsWith(prefix))
            .OrderByDescending(o => o.Number)
            .Select(o => o.Number)
            .FirstOrDefaultAsync(ct);

        return OrderNumberGenerator.Next(year, last);
    }

    private static IQueryable<Order> ApplySort(IQueryable<Order> query, string? sort) =>
        (sort ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "number" => query.OrderBy(o => o.Number),
            "-number" => query.OrderByDescending(o => o.Number),
            "createdat" => query.OrderBy(o => o.CreatedAt),
            "updatedat" => query.OrderBy(o => o.UpdatedAt),
            "-updatedat" => query.OrderByDescending(o => o.UpdatedAt),
            "status" => query.OrderBy(o => o.Status).ThenByDescending(o => o.CreatedAt),
            "-status" => query.OrderByDescending(o => o.Status).ThenByDescending(o => o.CreatedAt),
            "priority" => query.OrderBy(o => o.Priority).ThenByDescending(o => o.CreatedAt),
            "-priority" => query.OrderByDescending(o => o.Priority).ThenByDescending(o => o.CreatedAt),
            _ => query.OrderByDescending(o => o.CreatedAt)
        };

    /// <summary>Экранирование спецсимволов LIKE, чтобы «%» в поиске искался как символ, а не как шаблон.</summary>
    private static string EscapeLike(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}
