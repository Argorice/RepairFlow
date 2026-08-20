using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RepairFlow.Api.Caching;
using RepairFlow.Api.Common;
using RepairFlow.Api.Configuration;
using RepairFlow.Api.Contracts;
using RepairFlow.Api.Data;
using RepairFlow.Api.Domain;
using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(DateTime? from, DateTime? to, CancellationToken ct = default);
}

public sealed class DashboardService : IDashboardService
{
    /// <summary>Сколько дней согласование сметы считается «зависшим».</summary>
    private const int EstimateStaleDays = 3;

    private const int DefaultPeriodDays = 30;

    private readonly AppDbContext _db;
    private readonly ICacheStore _cache;
    private readonly CacheOptions _options;

    public DashboardService(AppDbContext db, ICacheStore cache, IOptions<CacheOptions> options)
    {
        _db = db;
        _cache = cache;
        _options = options.Value;
    }

    /// <summary>
    /// Сводка считается десятком запросов к базе, а меняется медленно, поэтому результат кладётся
    /// в кеш на минуту. Сериализуется он MessagePack'ом — DTO со списками по дням в JSON заметно толще.
    /// </summary>
    public Task<DashboardSummaryDto> GetSummaryAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return BuildSummaryAsync(from, to, ct);
        }

        // Ключ строится по исходным параметрам запроса, а не по вычисленному периоду:
        // иначе «последние 30 дней» давали бы новый ключ на каждый запрос и кеш никогда бы не срабатывал.
        var key = $"dashboard:{Stamp(from)}:{Stamp(to)}";

        return _cache.GetOrCreateAsync(
            key,
            TimeSpan.FromSeconds(_options.DashboardSeconds),
            token => BuildSummaryAsync(from, to, token),
            ct);
    }

    private static string Stamp(DateTime? value) =>
        value?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "default";

    private async Task<DashboardSummaryDto> BuildSummaryAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var periodTo = DateRange.EndOfDay(to) ?? now;
        var periodFrom = DateRange.ToUtc(from) ?? periodTo.AddDays(-DefaultPeriodDays).Date;

        if (periodFrom > periodTo)
        {
            (periodFrom, periodTo) = (periodTo, periodFrom);
        }

        var byStatus = await _db.Orders.AsNoTracking()
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var statusCounts = Enum.GetValues<OrderStatus>()
            .Select(status => new StatusCountDto(
                status,
                OrderStatusMachine.Describe(status),
                byStatus.FirstOrDefault(x => x.Status == status)?.Count ?? 0))
            .ToList();

        var totalOrders = byStatus.Sum(x => x.Count);

        var openOrders = statusCounts
            .Where(s => !OrderStatusMachine.IsTerminal(s.Status))
            .Sum(s => s.Count);

        var staleBefore = now.AddDays(-EstimateStaleDays);
        var overdueEstimates = await _db.Orders.AsNoTracking()
            .CountAsync(o => o.Status == OrderStatus.AwaitingEstimateApproval && o.UpdatedAt < staleBefore, ct);

        // Завершённые за период тянем одной выборкой: дальше по ним считаются и выручка, и средний срок.
        var completed = await _db.Orders.AsNoTracking()
            .Where(o => o.CompletedAt != null && o.CompletedAt >= periodFrom && o.CompletedAt <= periodTo)
            .Select(o => new CompletedProjection(
                o.CreatedAt,
                o.CompletedAt!.Value,
                o.FinalCost,
                o.AssignedTechnicianId))
            .ToListAsync(ct);

        var revenue = completed.Sum(c => c.FinalCost ?? 0m);

        double? averageHours = completed.Count == 0
            ? null
            : Math.Round(completed.Average(c => (c.CompletedAt - c.CreatedAt).TotalHours), 1);

        var createdPerDay = await _db.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= periodFrom && o.CreatedAt <= periodTo)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var completedPerDay = completed
            .GroupBy(c => c.CompletedAt.Date)
            .ToDictionary(g => DateOnly.FromDateTime(g.Key), g => g.Count());

        var createdMap = createdPerDay.ToDictionary(x => DateOnly.FromDateTime(x.Day), x => x.Count);

        var daily = new List<DailyCountDto>();
        for (var day = DateOnly.FromDateTime(periodFrom); day <= DateOnly.FromDateTime(periodTo); day = day.AddDays(1))
        {
            daily.Add(new DailyCountDto(
                day,
                createdMap.GetValueOrDefault(day),
                completedPerDay.GetValueOrDefault(day)));
        }

        var technicians = await _db.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Technician)
            .Select(u => new { u.Id, u.FullName, u.IsActive })
            .ToListAsync(ct);

        var activeByTechnician = await _db.Orders.AsNoTracking()
            .Where(o => o.AssignedTechnicianId != null
                        && o.Status != OrderStatus.Completed
                        && o.Status != OrderStatus.Cancelled
                        && o.Status != OrderStatus.ClientRejected)
            .GroupBy(o => o.AssignedTechnicianId!.Value)
            .Select(g => new { TechnicianId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var load = technicians
            .Select(t => new TechnicianLoadDto(
                t.Id,
                t.FullName,
                activeByTechnician.FirstOrDefault(a => a.TechnicianId == t.Id)?.Count ?? 0,
                completed.Count(c => c.TechnicianId == t.Id),
                completed.Where(c => c.TechnicianId == t.Id).Sum(c => c.FinalCost ?? 0m)))
            .OrderByDescending(t => t.ActiveOrders)
            .ThenBy(t => t.FullName)
            .ToList();

        return new DashboardSummaryDto(
            periodFrom,
            periodTo,
            totalOrders,
            openOrders,
            overdueEstimates,
            completed.Count,
            revenue,
            averageHours,
            statusCounts,
            daily,
            load);
    }

    /// <summary>Проекция завершённых заявок — чтобы из базы уехало ровно четыре нужных поля.</summary>
    private sealed record CompletedProjection(
        DateTime CreatedAt,
        DateTime CompletedAt,
        decimal? FinalCost,
        Guid? TechnicianId);
}
