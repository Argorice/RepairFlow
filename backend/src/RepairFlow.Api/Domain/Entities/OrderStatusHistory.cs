using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Domain.Entities;

/// <summary>Аудит переходов статуса: кто, когда и с каким комментарием сменил статус заявки.</summary>
public class OrderStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }

    public Order Order { get; set; } = null!;

    /// <summary>Исходный статус. null — запись о создании заявки.</summary>
    public OrderStatus? FromStatus { get; set; }

    public OrderStatus ToStatus { get; set; }

    public Guid ChangedById { get; set; }

    public User ChangedBy { get; set; } = null!;

    public string? Comment { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
