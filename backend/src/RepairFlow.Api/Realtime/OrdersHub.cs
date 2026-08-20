using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RepairFlow.Api.Authorization;
using RepairFlow.Api.Data;
using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Realtime;

/// <summary>
/// Хаб живых обновлений заявки. Клиент подписывается на конкретную заявку и получает события
/// смены статуса и новых комментариев без перезагрузки страницы.
/// Протокол — MessagePack: те же события в JSON занимают примерно вдвое больше.
/// </summary>
[Authorize]
public sealed class OrdersHub : Hub
{
    public const string Path = "/hubs/orders";

    /// <summary>Имя метода, которое слушает клиент.</summary>
    public const string EventMethod = "orderEvent";

    private readonly AppDbContext _db;

    public OrdersHub(AppDbContext db) => _db = db;

    /// <summary>Группа всех, кто видит заявку, включая клиента.</summary>
    public static string OrderGroup(Guid orderId) => $"order:{orderId}";

    /// <summary>Группа только сотрудников: сюда уходят внутренние заметки.</summary>
    public static string StaffGroup(Guid orderId) => $"order:{orderId}:staff";

    /// <summary>
    /// Подписка на заявку. Права проверяются тем же правилом, что и в REST, —
    /// подключение к хабу не должно быть обходным путём к чужим данным.
    /// </summary>
    public async Task SubscribeToOrder(Guid orderId)
    {
        if (!TryGetIdentity(out var userId, out var role))
        {
            throw new HubException("Не удалось определить пользователя.");
        }

        var order = await _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, Context.ConnectionAborted);

        if (order is null || !OrderAccessHandler.IsGranted(role, userId, order, OrderAccessLevel.Read))
        {
            throw new HubException("Заявка недоступна.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, OrderGroup(orderId), Context.ConnectionAborted);

        if (role != UserRole.Client)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, StaffGroup(orderId), Context.ConnectionAborted);
        }
    }

    public async Task UnsubscribeFromOrder(Guid orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, OrderGroup(orderId), Context.ConnectionAborted);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, StaffGroup(orderId), Context.ConnectionAborted);
    }

    private bool TryGetIdentity(out Guid userId, out UserRole role)
    {
        userId = Guid.Empty;
        role = default;

        var principal = Context.User;
        if (principal is null)
        {
            return false;
        }

        var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;

        return Guid.TryParse(idClaim, out userId) && Enum.TryParse(roleClaim, out role);
    }
}
