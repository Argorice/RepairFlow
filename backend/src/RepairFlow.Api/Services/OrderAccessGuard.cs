using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RepairFlow.Api.Authorization;
using RepairFlow.Api.Common;
using RepairFlow.Api.Data;
using RepairFlow.Api.Domain.Entities;

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
                        ?? throw new UnauthorizedException("Запрос выполнен вне контекста пользователя.");

        var result = await _authorization.AuthorizeAsync(principal, order, requirement);

        if (!result.Succeeded)
        {
            throw new ForbiddenException($"Нет прав на заявку {order.Number}.");
        }
    }

    public async Task<Order> LoadOrderAsync(
        Guid orderId,
        OrderAccessRequirement requirement,
        CancellationToken ct = default)
    {
        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId, ct)
                    ?? throw NotFoundException.For("Заявка", orderId);

        await EnsureAsync(order, requirement);

        return order;
    }
}
