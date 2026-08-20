namespace RepairFlow.Api.Domain.Enums;

/// <summary>Роль пользователя в системе. Определяет набор доступных операций.</summary>
public enum UserRole
{
    /// <summary>Клиент сервисного центра: создаёт заявки и следит за своими.</summary>
    Client = 0,

    /// <summary>Мастер: работает по назначенным заявкам.</summary>
    Technician = 1,

    /// <summary>Менеджер: видит всё, назначает мастеров, управляет пользователями.</summary>
    Manager = 2
}
