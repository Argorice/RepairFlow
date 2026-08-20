using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Contracts;

public sealed record CreateOrderRequest(
    string DeviceType,
    string Brand,
    string Model,
    string? SerialNumber,
    string ProblemDescription,
    OrderPriority? Priority);

/// <summary>PATCH: передаются только изменяемые поля, остальные остаются как были.</summary>
public sealed record UpdateOrderRequest(
    string? DeviceType,
    string? Brand,
    string? Model,
    string? SerialNumber,
    string? ProblemDescription,
    OrderPriority? Priority);

public sealed record ChangeStatusRequest(OrderStatus Status, string? Comment);

/// <summary>Назначение мастера. null снимает назначение — это допустимая операция менеджера.</summary>
public sealed record AssignTechnicianRequest(Guid? TechnicianId);

/// <summary>Вариант перехода для кнопок на фронтенде.</summary>
public sealed record StatusOptionDto(OrderStatus Status, string Label);

public sealed record OrderListItemDto(
    Guid Id,
    string Number,
    string DeviceType,
    string Brand,
    string Model,
    OrderStatus Status,
    string StatusLabel,
    OrderPriority Priority,
    UserSummaryDto Client,
    UserSummaryDto? Technician,
    decimal? EstimatedCost,
    decimal? FinalCost,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt);

public sealed record OrderDetailsDto(
    Guid Id,
    string Number,
    string DeviceType,
    string Brand,
    string Model,
    string? SerialNumber,
    string ProblemDescription,
    OrderStatus Status,
    string StatusLabel,
    OrderPriority Priority,
    UserSummaryDto Client,
    UserSummaryDto? Technician,
    decimal? EstimatedCost,
    decimal? FinalCost,
    decimal EstimateTotal,
    int ItemsCount,
    int CommentsCount,
    int AttachmentsCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt,
    IReadOnlyList<StatusOptionDto> AvailableTransitions,
    bool CanEdit,
    bool CanManageEstimate);

public sealed record OrderStatusHistoryDto(
    Guid Id,
    OrderStatus? FromStatus,
    string? FromStatusLabel,
    OrderStatus ToStatus,
    string ToStatusLabel,
    UserSummaryDto ChangedBy,
    string? Comment,
    DateTime ChangedAt);

/// <summary>
/// Параметры выборки списка. Фильтры прилетают из query-строки и туда же возвращаются фронтендом,
/// чтобы ссылку на отфильтрованный список можно было переслать коллеге.
/// </summary>
public sealed class OrderQuery
{
    public OrderStatus? Status { get; set; }

    public OrderPriority? Priority { get; set; }

    public Guid? TechnicianId { get; set; }

    public Guid? ClientId { get; set; }

    /// <summary>Поиск по номеру, бренду, модели, серийному номеру и описанию проблемы.</summary>
    public string? Search { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    /// <summary>createdAt | updatedAt | number | status | priority, с префиксом «-» для убывания.</summary>
    public string? Sort { get; set; } = "-createdAt";
}
