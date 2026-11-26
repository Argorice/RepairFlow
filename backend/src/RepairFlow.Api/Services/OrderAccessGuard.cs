using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RepairFlow.Api.Authorization;
using RepairFlow.Api.Common;
using RepairFlow.Api.Data;
using RepairFlow.Api.Domain.Entities;
using RepairFlow.Api.Localization;

namespace RepairFlow.Api.Services;

/// <summary>
/// Тонкая обёртка над resource-based авторизацией: сервисы спрашивают «можно ли этому пользователю
/// такое с этой заявкой» и получают исключение вместо булева результата.
/// </summary>
public interface IOrderAccessGuard
{
    Task EnsureAsync(Order order, OrderAccessRequirement requirement);

    /// <summary>Загрузить заявку и сразу проверить права на неё. Нет заявки — 404, нет прав — 403.</summary>
    Task<Order> LoadOrderAsync(Guid orderId, OrderAccessRequirement requirement, CancellationToken ct = default);
}

public sealed class OrderAccessGuard : IOrderAccessGuard
{
    private readonly IAuthorizationService _authorization;
    private readonly IHttpContextAccessor _httpContext;
    private readonly AppDbContext _db;

    public OrderAccessGuard(
        IAuthorizationService authorization,
        IHttpContextAccessor httpContext,
        AppDbContext db)
    {
        _authorization = authorization;
        _httpContext = httpContext;
        _db = db;
    }

    public async Task EnsureAsync(Order order, OrderAccessRequirement requirement)
    {
        var principal = _httpContext.HttpContext?.User
                        ?? throw new UnauthorizedException(Texts.Pick("The request was made outside a user context.", "Запрос выполнен вне контекста пользователя."));

        var result = await _authorization.AuthorizeAsync(principal, order, requirement);

        if (!result.Succeeded)
        {
            throw new ForbiddenException(Texts.Pick($"You don’t have access to request {order.Number}.", $"Нет прав на заявку {order.Number}."));
        }
    }

    public async Task<Order> LoadOrderAsync(
        Guid orderId,
        OrderAccessRequirement requirement,
        CancellationToken ct = default)
    {
        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId, ct)
                    ?? throw NotFoundException.For(Texts.Pick("request", "заявка"), orderId);

        await EnsureAsync(order, requirement);

        return order;
    }
}
