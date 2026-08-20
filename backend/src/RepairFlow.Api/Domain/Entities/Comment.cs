namespace RepairFlow.Api.Domain.Entities;

public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public Guid AuthorId { get; set; }

    public User Author { get; set; } = null!;

    public string Text { get; set; } = null!;

    /// <summary>Внутренняя заметка: видна только мастеру и менеджеру, клиенту — нет.</summary>
    public bool IsInternal { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
