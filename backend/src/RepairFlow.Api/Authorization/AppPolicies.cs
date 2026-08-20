using Microsoft.AspNetCore.Authorization;
using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Authorization;

/// <summary>
/// Политики авторизации. Контроллеры ссылаются на имена политик, а не пишут
/// «if (user.Role == Manager)» — правило живёт в одном месте и меняется тоже в одном.
/// </summary>
public static class AppPolicies
{
    /// <summary>Только менеджер: пользователи, назначения, аналитика.</summary>
    public const string ManagerOnly = "ManagerOnly";

    /// <summary>Сотрудники сервиса: мастер и менеджер.</summary>
    public const string StaffOnly = "StaffOnly";

    /// <summary>Только клиент: создание заявки, подтверждение и отклонение сметы.</summary>
    public const string ClientOnly = "ClientOnly";

    /// <summary>Любой аутентифицированный активный пользователь.</summary>
    public const string Authenticated = "Authenticated";

    /// <summary>
    /// Имя CORS-политики для фронтенда. Лежит здесь, а не в классе расширений DI:
    /// у FluentValidation есть свой ServiceCollectionExtensions, и обращение к нашему
    /// по короткому имени становится неоднозначным.
    /// </summary>
    public const string CorsPolicy = "RepairFlowFrontend";

    public static AuthorizationOptions AddAppPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(ManagerOnly, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(nameof(UserRole.Manager)));

        options.AddPolicy(StaffOnly, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(nameof(UserRole.Technician), nameof(UserRole.Manager)));

        options.AddPolicy(ClientOnly, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(nameof(UserRole.Client)));

        options.AddPolicy(Authenticated, policy => policy.RequireAuthenticatedUser());

        options.DefaultPolicy = options.GetPolicy(Authenticated)!;

        return options;
    }
}
