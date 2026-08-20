using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Contracts;

public sealed record StatusCountDto(OrderStatus Status, string Label, int Count);

public sealed record DailyCountDto(DateOnly Date, int Created, int Completed);

public sealed record TechnicianLoadDto(
    Guid TechnicianId,
    string FullName,
    int ActiveOrders,
    int CompletedInPeriod,
    decimal RevenueInPeriod);

public sealed record DashboardSummaryDto(
    DateTime From,
    DateTime To,
    int TotalOrders,
    int OpenOrders,
    int OverdueEstimates,
    int CompletedInPeriod,
    decimal RevenueInPeriod,
    double? AverageRepairHours,
    IReadOnlyList<StatusCountDto> ByStatus,
    IReadOnlyList<DailyCountDto> Daily,
    IReadOnlyList<TechnicianLoadDto> TechnicianLoad);
