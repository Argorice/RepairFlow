using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Domain.Entities;

/// <summary>Заявка на ремонт — центральная сущность системы.</summary>
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Человекочитаемый номер вида RF-2026-0001, уникален и последователен в рамках года.</summary>
    public string Number { get; set; } = null!;

    public Guid ClientId { get; set; }

    public User Client { get; set; } = null!;

    public Guid? AssignedTechnicianId { get; set; }

    public User? AssignedTechnician { get; set; }

    public string DeviceType { get; set; } = null!;

    public string Brand { get; set; } = null!;

    public string Model { get; set; } = null!;

    public string? SerialNumber { get; set; }

    public string ProblemDescription { get; set; } = null!;

    public OrderStatus Status { get; set; } = OrderStatus.New;

    public OrderPriority Priority { get; set; } = OrderPriority.Normal;

    /// <summary>Предварительная стоимость: сумма позиций сметы на момент отправки клиенту.</summary>
    public decimal? EstimatedCost { get; set; }

    /// <summary>Итоговая стоимость: фиксируется при выдаче заявки.</summary>
    public decimal? FinalCost { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Момент выдачи заявки клиенту. Используется для расчёта среднего срока ремонта.</summary>
    public DateTime? CompletedAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public ICollection<OrderStatusHistory> History { get; set; } = new List<OrderStatusHistory>();

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
