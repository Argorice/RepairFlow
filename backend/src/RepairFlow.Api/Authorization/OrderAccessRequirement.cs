using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RepairFlow.Api.Domain.Entities;
using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Authorization;

/// <summary>Уровень доступа к конкретной заявке.</summary>
public enum OrderAccessLevel
{
    /// <summary>Просмотр заявки и её вложенных данных.</summary>
    Read,

    /// <summary>Изменение заявки: поля, статус, смета, файлы.</summary>
    Write,

    /// <summary>Административные действия: назначение мастера.</summary>
    Manage
}

/// <summary>
/// Requirement для авторизации на уровне ресурса. Правило «клиент видит только свои заявки,
/// мастер — назначенные ему и свободные» описано здесь один раз и переиспользуется всеми сервисами,
/// которые работают с одной заявкой.
/// </summary>
public sealed class OrderAccessRequirement : IAuthorizationRequirement
{
    public OrderAccessRequirement(OrderAccessLevel level) => Level = level;

    public OrderAccessLevel Level { get; }

    public static readonly OrderAccessRequirement Read = new(OrderAccessLevel.Read);

    public static readonly OrderAccessRequirement Write = new(OrderAccessLevel.Write);

    public static readonly OrderAccessRequirement Manage = new(OrderAccessLevel.Manage);
}

public sealed class OrderAccessHandler : AuthorizationHandler<OrderAccessRequirement, Order>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrderAccessRequirement requirement,
        Order resource)
    {
        if (!TryGetIdentity(context.User, out var userId, out var role))
        {
            return Task.CompletedTask;
        }

        if (IsGranted(role, userId, resource, requirement.Level))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    /// <summary>Правило вынесено в статический метод, чтобы его можно было проверить юнит-тестом без HTTP-контекста.</summary>
    public static bool IsGranted(UserRole role, Guid userId, Order order, OrderAccessLevel level) => role switch
    {
        UserRole.Manager => true,

        // Мастер работает со своими заявками и с общим пулом ещё не назначенных.
        UserRole.Technician => level != OrderAccessLevel.Manage
                               && (order.AssignedTechnicianId == userId || order.AssignedTechnicianId is null),

        // Клиент — только свои заявки. Что именно ему разрешено делать, решает машина состояний.
        UserRole.Client => level != OrderAccessLevel.Manage && order.ClientId == userId,

        _ => false
    };

    private static bool TryGetIdentity(ClaimsPrincipal principal, out Guid userId, out UserRole role)
    {
        userId = Guid.Empty;
        role = default;

        var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;

        return Guid.TryParse(idClaim, out userId) && Enum.TryParse(roleClaim, out role);
    }
}
