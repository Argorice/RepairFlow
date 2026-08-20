namespace RepairFlow.Api.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Корневая папка файлового хранилища относительно рабочего каталога приложения.</summary>
    public string Root { get; set; } = "storage";

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>Белый список MIME-типов: всё остальное отклоняется до записи на диск.</summary>
    public string[] AllowedContentTypes { get; set; } =
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/heic",
        "application/pdf"
    };

    /// <summary>Белый список расширений — вторая проверка, независимая от заголовка Content-Type.</summary>
    public string[] AllowedExtensions { get; set; } =
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".heic",
        ".pdf"
    };

    public int MaxAttachmentsPerOrder { get; set; } = 20;
}
