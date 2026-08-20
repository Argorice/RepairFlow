using Microsoft.Extensions.Options;
using RepairFlow.Api.Configuration;

namespace RepairFlow.Api.Services;

/// <summary>
/// Файловое хранилище на диске. Интерфейс намеренно узкий: если проект переедет на S3,
/// меняется одна реализация, а сервисы остаются как есть.
/// </summary>
public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken ct = default);

    Stream OpenRead(string relativePath);

    void Delete(string relativePath);

    bool Exists(string relativePath);
}

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<StorageOptions> options, IHostEnvironment environment)
    {
        var configured = options.Value.Root;

        _root = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured);

        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken ct = default)
    {
        // Имя файла генерируем сами — так исходное имя пользователя не попадает в файловую систему
        // и обход каталогов через «../» становится невозможен в принципе.
        var now = DateTime.UtcNow;
        var folder = Path.Combine(now.Year.ToString("D4"), now.Month.ToString("D2"));
        var fileName = Guid.NewGuid().ToString("N") + NormalizeExtension(extension);
        var relativePath = Path.Combine(folder, fileName);

        var absoluteFolder = Path.Combine(_root, folder);
        Directory.CreateDirectory(absoluteFolder);

        await using var file = File.Create(Path.Combine(_root, relativePath));
        await content.CopyToAsync(file, ct);

        return relativePath.Replace(Path.DirectorySeparatorChar, '/');
    }

    public Stream OpenRead(string relativePath) =>
        File.OpenRead(Resolve(relativePath));

    public void Delete(string relativePath)
    {
        var path = Resolve(relativePath);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public bool Exists(string relativePath) => File.Exists(Resolve(relativePath));

    /// <summary>Путь всегда собирается от корня хранилища и проверяется на выход за его пределы.</summary>
    private string Resolve(string relativePath)
    {
        var combined = Path.GetFullPath(Path.Combine(_root, relativePath));

        if (!combined.StartsWith(Path.GetFullPath(_root), StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Попытка выйти за пределы файлового хранилища.");
        }

        return combined;
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        var trimmed = extension.Trim().ToLowerInvariant();
        return trimmed.StartsWith('.') ? trimmed : "." + trimmed;
    }
}
