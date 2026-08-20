using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Domain.Entities;

/// <summary>Позиция сметы: запчасть или работа.</summary>
public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public OrderItemType Type { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>Количество штук или нормо-часов. Дробное — часы бывают неполными.</summary>
    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
