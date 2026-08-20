namespace RepairFlow.Api.Domain.Entities;

/// <summary>Файл, приложенный к заявке. Сам файл лежит в хранилище, в БД — только метаданные.</summary>
public class Attachment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }

    public Order Order { get; set; } = null!;

    /// <summary>Оригинальное имя файла, как его загрузил пользователь.</summary>
    public string FileName { get; set; } = null!;

    /// <summary>Относительный путь внутри хранилища. Наружу никогда не отдаётся.</summary>
    public string StoredPath { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long SizeBytes { get; set; }

    public Guid UploadedById { get; set; }

    public User UploadedBy { get; set; } = null!;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
