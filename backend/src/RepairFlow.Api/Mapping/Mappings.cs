using RepairFlow.Api.Contracts;
using RepairFlow.Api.Domain;
using RepairFlow.Api.Domain.Entities;
using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Mapping;

/// <summary>
/// Маппинг сущностей в DTO вручную. Библиотека здесь ничего бы не выиграла:
/// правил немного, зато видно, какие поля наружу не уезжают (PasswordHash, StoredPath, IsInternal-заметки).
/// </summary>
public static class Mappings
{
    public static UserDto ToDto(this User user) => new(
        user.Id,
        user.Email,
        user.FullName,
        user.Phone,
        user.Role,
        OrderStatusMachine.Describe(user.Role),
        user.IsActive,
        user.CreatedAt);

    public static UserSummaryDto ToSummary(this User user) => new(
        user.Id,
        user.FullName,
        user.Email,
        user.Role);

    public static UserSummaryDto? ToSummaryOrNull(this User? user) => user?.ToSummary();

    public static OrderListItemDto ToListItem(this Order order) => new(
        order.Id,
        order.Number,
        order.DeviceType,
        order.Brand,
        order.Model,
        order.Status,
        OrderStatusMachine.Describe(order.Status),
        order.Priority,
        order.Client.ToSummary(),
        order.AssignedTechnician.ToSummaryOrNull(),
        order.EstimatedCost,
        order.FinalCost,
        order.CreatedAt,
        order.UpdatedAt,
        order.CompletedAt);

    public static OrderItemDto ToDto(this OrderItem item) => new(
        item.Id,
        item.Type,
        item.Type == OrderItemType.Part ? "Запчасть" : "Работа",
        item.Name,
        item.Quantity,
        item.UnitPrice,
        EstimateCalculator.LineTotal(item));

    public static CommentDto ToDto(this Comment comment) => new(
        comment.Id,
        comment.Text,
        comment.IsInternal,
        comment.Author.ToSummary(),
        comment.CreatedAt);

    public static AttachmentDto ToDto(this Attachment attachment, string url) => new(
        attachment.Id,
        attachment.OrderId,
        attachment.FileName,
        attachment.ContentType,
        attachment.SizeBytes,
        attachment.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase),
        attachment.UploadedBy.ToSummary(),
        attachment.UploadedAt,
        url);

    public static OrderStatusHistoryDto ToDto(this OrderStatusHistory history) => new(
        history.Id,
        history.FromStatus,
        history.FromStatus is null ? null : OrderStatusMachine.Describe(history.FromStatus.Value),
        history.ToStatus,
        OrderStatusMachine.Describe(history.ToStatus),
        history.ChangedBy.ToSummary(),
        history.Comment,
        history.ChangedAt);

    public static StatusOptionDto ToOption(this OrderStatus status) =>
        new(status, OrderStatusMachine.Describe(status));
}
