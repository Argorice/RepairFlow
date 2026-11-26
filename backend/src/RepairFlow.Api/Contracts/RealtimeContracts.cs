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
///
/// Текстов на чьём-то языке здесь нет. Событие рассылается группе, а участники группы читают
/// интерфейс на разных языках, — поэтому едут только коды (<see cref="Kind"/>, <see cref="Status"/>),
/// а подпись собирает каждый клиент сам.
/// </summary>
/// <param name="Message">
/// Только то, что написал человек: превью комментария или комментарий к смене статуса.
/// У назначения мастера сообщения нет — null.
/// </param>
public sealed record OrderEventDto(
    Guid OrderId,
    string Number,
    OrderEventKind Kind,
    OrderStatus Status,
    string? Message,
    DateTime At);
