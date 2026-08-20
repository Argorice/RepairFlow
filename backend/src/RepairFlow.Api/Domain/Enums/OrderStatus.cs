namespace RepairFlow.Api.Domain.Enums;

/// <summary>
/// Статус заявки. Основной путь:
/// New → Diagnostics → AwaitingEstimateApproval → InProgress → ReadyForPickup → Completed.
/// Тупиковые: Cancelled, ClientRejected.
/// </summary>
public enum OrderStatus
{
    /// <summary>Новая — принята, но ещё не взята в диагностику.</summary>
    New = 0,

    /// <summary>Диагностика — мастер определяет причину неисправности.</summary>
    Diagnostics = 1,

    /// <summary>Ожидает подтверждения сметы клиентом.</summary>
    AwaitingEstimateApproval = 2,

    /// <summary>В работе — смета согласована, идёт ремонт.</summary>
    InProgress = 3,

    /// <summary>Готова к выдаче.</summary>
    ReadyForPickup = 4,

    /// <summary>Выдана клиенту — заявка закрыта успешно.</summary>
    Completed = 5,

    /// <summary>Отменена (клиентом в статусе «Новая» или менеджером).</summary>
    Cancelled = 6,

    /// <summary>Отказ клиента — смета не согласована.</summary>
    ClientRejected = 7
}
