using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Contracts;

public sealed record OrderItemDto(
    Guid Id,
    OrderItemType Type,
    string TypeLabel,
    string Name,
    decimal Quantity,
    decimal UnitPrice,
    decimal Total);

public sealed record SaveOrderItemRequest(OrderItemType Type, string Name, decimal Quantity, decimal UnitPrice);

/// <summary>Смета целиком: позиции плюс подсчитанные итоги, чтобы фронтенд ничего не пересчитывал сам.</summary>
public sealed record EstimateDto(
    IReadOnlyList<OrderItemDto> Items,
    decimal PartsTotal,
    decimal LaborTotal,
    decimal Total,
    bool IsEditable,
    bool AwaitingApproval);

public sealed record RejectEstimateRequest(string? Reason);
