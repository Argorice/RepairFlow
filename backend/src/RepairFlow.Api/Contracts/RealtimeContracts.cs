using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Contracts;

/// <summary>Что именно произошло с заявкой.</summary>
public enum OrderEventKind
{
    StatusChanged = 0,
    TechnicianAssigned = 1,
    CommentAdded = 2
}

/// <summary>
/// Событие по заявке, которое уезжает в браузер через SignalR.
/// Payload намеренно плоский и маленький: получатель по нему обновляет карточку, а не строит её заново.
/// </summary>
public sealed record OrderEventDto(
    Guid OrderId,
    string Number,
    OrderEventKind Kind,
    OrderStatus Status,
    string StatusLabel,
    string? Message,
    DateTime At);
