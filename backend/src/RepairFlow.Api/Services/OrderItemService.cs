using Microsoft.EntityFrameworkCore;
using RepairFlow.Api.Authorization;
using RepairFlow.Api.Common;
using RepairFlow.Api.Contracts;
using RepairFlow.Api.Data;
using RepairFlow.Api.Domain;
using RepairFlow.Api.Domain.Entities;
using RepairFlow.Api.Domain.Enums;
using RepairFlow.Api.Mapping;

namespace RepairFlow.Api.Services;

public interface IOrderItemService
{
    Task<EstimateDto> GetAsync(Guid orderId, CancellationToken ct = default);

    Task<OrderItemDto> AddAsync(Guid orderId, SaveOrderItemRequest request, CancellationToken ct = default);

    Task<OrderItemDto> UpdateAsync(Guid orderId, Guid itemId, SaveOrderItemRequest request, CancellationToken ct = default);

    Task DeleteAsync(Guid orderId, Guid itemId, CancellationToken ct = default);

    Task<OrderDetailsDto> ApproveAsync(Guid orderId, CancellationToken ct = default);

    Task<OrderDetailsDto> RejectAsync(Guid orderId, RejectEstimateRequest request, CancellationToken ct = default);
}

public sealed class OrderItemService : IOrderItemService
{
    private readonly AppDbContext _db;
    private readonly IOrderAccessGuard _guard;
    private readonly ICurrentUser _currentUser;
    private readonly IOrderService _orders;

    public OrderItemService(
        AppDbContext db,
        IOrderAccessGuard guard,
        ICurrentUser currentUser,
        IOrderService orders)
    {
        _db = db;
        _guard = guard;
        _currentUser = currentUser;
        _orders = orders;
    }

    public async Task<EstimateDto> GetAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _guard.LoadOrderAsync(orderId, OrderAccessRequirement.Read, ct);

        var items = await _db.OrderItems.AsNoTracking()
            .Where(i => i.OrderId == orderId)
            .OrderBy(i => i.Type)
            .ThenBy(i => i.CreatedAt)
            .ToListAsync(ct);

        var totals = EstimateCalculator.Calculate(items);

        return new EstimateDto(
            items.Select(i => i.ToDto()).ToList(),
            totals.PartsTotal,
            totals.LaborTotal,
            totals.Total,
            IsEditable: !_currentUser.IsClient && OrderStatusMachine.IsEstimateEditable(order.Status),
            AwaitingApproval: order.Status == OrderStatus.AwaitingEstimateApproval);
    }

    public async Task<OrderItemDto> AddAsync(Guid orderId, SaveOrderItemRequest request, CancellationToken ct = default)
    {
        var order = await LoadEditableAsync(orderId, ct);

        var item = new OrderItem
        {
            OrderId = order.Id,
            Type = request.Type,
            Name = request.Name.Trim(),
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            CreatedAt = DateTime.UtcNow
        };

        _db.OrderItems.Add(item);
        await TouchOrderAsync(order.Id, ct);
        await _db.SaveChangesAsync(ct);

        return item.ToDto();
    }

    public async Task<OrderItemDto> UpdateAsync(
        Guid orderId,
        Guid itemId,
        SaveOrderItemRequest request,
        CancellationToken ct = default)
    {
        var order = await LoadEditableAsync(orderId, ct);

        var item = await _db.OrderItems.FirstOrDefaultAsync(i => i.Id == itemId && i.OrderId == order.Id, ct)
                   ?? throw NotFoundException.For("Позиция сметы", itemId);

        item.Type = request.Type;
        item.Name = request.Name.Trim();
        item.Quantity = request.Quantity;
        item.UnitPrice = request.UnitPrice;

        await TouchOrderAsync(order.Id, ct);
        await _db.SaveChangesAsync(ct);

        return item.ToDto();
    }

    public async Task DeleteAsync(Guid orderId, Guid itemId, CancellationToken ct = default)
    {
        var order = await LoadEditableAsync(orderId, ct);

        var item = await _db.OrderItems.FirstOrDefaultAsync(i => i.Id == itemId && i.OrderId == order.Id, ct)
                   ?? throw NotFoundException.For("Позиция сметы", itemId);

        _db.OrderItems.Remove(item);
        await TouchOrderAsync(order.Id, ct);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Подтверждение сметы клиентом — это просто переход статуса, поэтому логика не дублируется.</summary>
    public Task<OrderDetailsDto> ApproveAsync(Guid orderId, CancellationToken ct = default) =>
        _orders.ChangeStatusAsync(
            orderId,
            new ChangeStatusRequest(OrderStatus.InProgress, "Смета подтверждена клиентом"),
            ct);

    public Task<OrderDetailsDto> RejectAsync(Guid orderId, RejectEstimateRequest request, CancellationToken ct = default) =>
        _orders.ChangeStatusAsync(
            orderId,
            new ChangeStatusRequest(
                OrderStatus.ClientRejected,
                string.IsNullOrWhiteSpace(request.Reason) ? "Клиент отказался от ремонта" : request.Reason.Trim()),
            ct);

    private async Task<Order> LoadEditableAsync(Guid orderId, CancellationToken ct)
    {
        var order = await _guard.LoadOrderAsync(orderId, OrderAccessRequirement.Write, ct);

        if (_currentUser.IsClient)
        {
            throw new ForbiddenException("Смету формирует сервис. Клиент может её только подтвердить или отклонить.");
        }

        if (!OrderStatusMachine.IsEstimateEditable(order.Status))
        {
            throw new ConflictException(
                $"В статусе «{OrderStatusMachine.Describe(order.Status)}» смету менять нельзя.");
        }

        return order;
    }

    /// <summary>Правка сметы меняет заявку, поэтому обновляем её отметку времени — список сортируется по ней.</summary>
    private async Task TouchOrderAsync(Guid orderId, CancellationToken ct)
    {
        var tracked = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (tracked is not null)
        {
            tracked.UpdatedAt = DateTime.UtcNow;
        }
    }
}
