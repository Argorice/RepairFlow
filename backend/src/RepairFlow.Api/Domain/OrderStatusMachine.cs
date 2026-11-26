using RepairFlow.Api.Domain.Enums;
using RepairFlow.Api.Localization;

namespace RepairFlow.Api.Domain;

/// <summary>Результат проверки перехода. Если переход запрещён — содержит причину для ответа клиенту.</summary>
public readonly record struct TransitionCheck(bool IsAllowed, string? Error)
{
    public static TransitionCheck Ok() => new(true, null);

    public static TransitionCheck Fail(string error) => new(false, error);
}

/// <summary>
/// Машина состояний заявки. Чистая логика без зависимостей от БД и HTTP — поэтому её легко покрыть тестами.
/// Знает две вещи: какие переходы существуют вообще и кому из ролей они разрешены.
/// </summary>
public static class OrderStatusMachine
{
    /// <summary>Статусы, из которых нет выхода.</summary>
    public static readonly IReadOnlySet<OrderStatus> TerminalStatuses = new HashSet<OrderStatus>
    {
        OrderStatus.Completed,
        OrderStatus.Cancelled,
        OrderStatus.ClientRejected
    };

    /// <summary>
    /// Полный граф переходов: (откуда, куда) → роли, которым переход разрешён.
    /// Менеджер присутствует везде, но перечислен явно, чтобы правила читались одной таблицей,
    /// а не «менеджер может всё, кроме…».
    /// </summary>
    private static readonly IReadOnlyDictionary<(OrderStatus From, OrderStatus To), UserRole[]> Transitions =
        new Dictionary<(OrderStatus, OrderStatus), UserRole[]>
        {
            // Приём в работу
            [(OrderStatus.New, OrderStatus.Diagnostics)] = new[] { UserRole.Technician, UserRole.Manager },
            [(OrderStatus.New, OrderStatus.Cancelled)] = new[] { UserRole.Client, UserRole.Manager },

            // Диагностика → смета
            [(OrderStatus.Diagnostics, OrderStatus.AwaitingEstimateApproval)] = new[] { UserRole.Technician, UserRole.Manager },
            [(OrderStatus.Diagnostics, OrderStatus.Cancelled)] = new[] { UserRole.Manager },

            // Согласование сметы — решение за клиентом (менеджер может провести его же, например по телефону)
            [(OrderStatus.AwaitingEstimateApproval, OrderStatus.InProgress)] = new[] { UserRole.Client, UserRole.Manager },
            [(OrderStatus.AwaitingEstimateApproval, OrderStatus.ClientRejected)] = new[] { UserRole.Client, UserRole.Manager },
            // Смету можно отозвать и пересчитать
            [(OrderStatus.AwaitingEstimateApproval, OrderStatus.Diagnostics)] = new[] { UserRole.Technician, UserRole.Manager },

            // Ремонт
            [(OrderStatus.InProgress, OrderStatus.ReadyForPickup)] = new[] { UserRole.Technician, UserRole.Manager },
            [(OrderStatus.InProgress, OrderStatus.Cancelled)] = new[] { UserRole.Manager },

            // Выдача
            [(OrderStatus.ReadyForPickup, OrderStatus.Completed)] = new[] { UserRole.Technician, UserRole.Manager }
        };

    /// <summary>Существует ли такой переход в принципе (без учёта роли).</summary>
    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        Transitions.ContainsKey((from, to));

    public static bool IsTerminal(OrderStatus status) => TerminalStatuses.Contains(status);

    /// <summary>Все статусы, достижимые из указанного, без учёта роли.</summary>
    public static IReadOnlyList<OrderStatus> AllowedTargets(OrderStatus from) =>
        Transitions.Keys.Where(k => k.From == from).Select(k => k.To).OrderBy(s => (int)s).ToList();

    /// <summary>
    /// Статусы, в которые конкретная роль может перевести заявку. Фронтенд рисует по этому списку
    /// кнопки переходов, поэтому список отдаётся вместе с карточкой заявки.
    /// </summary>
    public static IReadOnlyList<OrderStatus> AllowedTargetsFor(UserRole role, OrderStatus from) =>
        Transitions
            .Where(kv => kv.Key.From == from && kv.Value.Contains(role))
            .Select(kv => kv.Key.To)
            .OrderBy(s => (int)s)
            .ToList();

    /// <summary>Проверка перехода с человекочитаемой причиной отказа на языке запроса.</summary>
    public static TransitionCheck Validate(UserRole role, OrderStatus from, OrderStatus to)
    {
        if (from == to)
        {
            return TransitionCheck.Fail(Texts.Pick(
                $"The request is already “{Describe(from)}”.",
                $"Заявка уже находится в статусе «{Describe(from)}»."));
        }

        if (IsTerminal(from))
        {
            return TransitionCheck.Fail(Texts.Pick(
                $"The request is closed (“{Describe(from)}”) and its status can’t change.",
                $"Заявка в статусе «{Describe(from)}» закрыта, менять статус нельзя."));
        }

        if (!Transitions.TryGetValue((from, to), out var roles))
        {
            return TransitionCheck.Fail(Texts.Pick(
                $"The process has no step from “{Describe(from)}” to “{Describe(to)}”.",
                $"Переход «{Describe(from)}» → «{Describe(to)}» не предусмотрен процессом."));
        }

        return roles.Contains(role)
            ? TransitionCheck.Ok()
            : TransitionCheck.Fail(Texts.Pick(
                $"A {Describe(role).ToLowerInvariant()} can’t move a request from “{Describe(from)}” to “{Describe(to)}”.",
                $"Роль «{Describe(role)}» не может выполнить переход «{Describe(from)}» → «{Describe(to)}»."));
    }

    /// <summary>Переход требует непустой сметы: нельзя отправить клиенту согласование пустого списка работ.</summary>
    public static bool RequiresNonEmptyEstimate(OrderStatus to) => to == OrderStatus.AwaitingEstimateApproval;

    /// <summary>Переход, после которого заявка считается закрытой успешно.</summary>
    public static bool IsCompletion(OrderStatus to) => to == OrderStatus.Completed;

    /// <summary>
    /// В каких статусах смету можно править. Пока клиент смотрит смету на согласовании,
    /// менять её под ним нельзя — сначала верните заявку в диагностику.
    /// </summary>
    public static bool IsEstimateEditable(OrderStatus status) =>
        status is OrderStatus.New or OrderStatus.Diagnostics or OrderStatus.InProgress;

    /// <summary>
    /// Название статуса на языке запроса — для сообщений об ошибках и подписей в ответах API.
    /// Совпадает с подписями фронтенда, чтобы ошибка говорила теми же словами, что и экран.
    /// </summary>
    public static string Describe(OrderStatus status) => Texts.IsRussian
        ? status switch
        {
            OrderStatus.New => "Новая",
            OrderStatus.Diagnostics => "Диагностика",
            OrderStatus.AwaitingEstimateApproval => "Ждёт согласования",
            OrderStatus.InProgress => "В ремонте",
            OrderStatus.ReadyForPickup => "Готова к выдаче",
            OrderStatus.Completed => "Выдана",
            OrderStatus.Cancelled => "Отменена",
            OrderStatus.ClientRejected => "Отказ клиента",
            _ => status.ToString()
        }
        : status switch
        {
            OrderStatus.New => "New",
            OrderStatus.Diagnostics => "Diagnostics",
            OrderStatus.AwaitingEstimateApproval => "Awaiting approval",
            OrderStatus.InProgress => "In repair",
            OrderStatus.ReadyForPickup => "Ready for pickup",
            OrderStatus.Completed => "Handed over",
            OrderStatus.Cancelled => "Cancelled",
            OrderStatus.ClientRejected => "Declined",
            _ => status.ToString()
        };

    public static string Describe(UserRole role) => role switch
    {
        UserRole.Client => Texts.Pick("Customer", "Клиент"),
        UserRole.Technician => Texts.Pick("Technician", "Мастер"),
        UserRole.Manager => Texts.Pick("Manager", "Менеджер"),
        _ => role.ToString()
    };
}
