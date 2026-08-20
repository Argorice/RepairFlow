using Microsoft.AspNetCore.SignalR;
using RepairFlow.Api.Contracts;
using RepairFlow.Api.Domain;
using RepairFlow.Api.Domain.Entities;
using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Realtime;

/// <summary>Рассылка событий по заявке. Сервисы зависят от интерфейса, а не от SignalR напрямую.</summary>
public interface IOrderNotifier
{
    Task StatusChangedAsync(Order order, OrderStatus from, string? comment, CancellationToken ct = default);

    Task TechnicianAssignedAsync(Order order, string? technicianName, CancellationToken ct = default);

    Task CommentAddedAsync(Order order, Comment comment, CancellationToken ct = default);
}

public sealed class OrderNotifier : IOrderNotifier
{
    private readonly IHubContext<OrdersHub> _hub;
    private readonly ILogger<OrderNotifier> _logger;

    public OrderNotifier(IHubContext<OrdersHub> hub, ILogger<OrderNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public Task StatusChangedAsync(Order order, OrderStatus from, string? comment, CancellationToken ct = default)
    {
        var message = $"{OrderStatusMachine.Describe(from)} → {OrderStatusMachine.Describe(order.Status)}"
                      + (string.IsNullOrWhiteSpace(comment) ? string.Empty : $": {comment}");

        return PublishAsync(order, OrderEventKind.StatusChanged, message, internalOnly: false, ct);
    }

    public Task TechnicianAssignedAsync(Order order, string? technicianName, CancellationToken ct = default) =>
        PublishAsync(
            order,
            OrderEventKind.TechnicianAssigned,
            technicianName is null ? "Мастер снят с заявки" : $"Назначен мастер: {technicianName}",
            internalOnly: false,
            ct);

    public Task CommentAddedAsync(Order order, Comment comment, CancellationToken ct = default) =>
        PublishAsync(
            order,
            OrderEventKind.CommentAdded,
            Preview(comment.Text),
            // Внутренняя заметка уходит только в группу сотрудников — клиент её не увидит даже здесь.
            internalOnly: comment.IsInternal,
            ct);

    private async Task PublishAsync(
        Order order,
        OrderEventKind kind,
        string? message,
        bool internalOnly,
        CancellationToken ct)
    {
        var payload = new OrderEventDto(
            order.Id,
            order.Number,
            kind,
            order.Status,
            OrderStatusMachine.Describe(order.Status),
            message,
            DateTime.UtcNow);

        var group = internalOnly ? OrdersHub.StaffGroup(order.Id) : OrdersHub.OrderGroup(order.Id);

        try
        {
            await _hub.Clients.Group(group).SendAsync(OrdersHub.EventMethod, payload, ct);
        }
        catch (Exception exception)
        {
            // Уведомление — приятный бонус, а не часть транзакции: его падение не должно ронять запрос.
            _logger.LogWarning(exception, "Не удалось разослать событие по заявке {Number}.", order.Number);
        }
    }

    private static string Preview(string text) =>
        text.Length <= 120 ? text : text[..117] + "…";
}
