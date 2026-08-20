using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RepairFlow.Api.Authorization;
using RepairFlow.Api.Common;
using RepairFlow.Api.Configuration;
using RepairFlow.Api.Contracts;
using RepairFlow.Api.Data;
using RepairFlow.Api.Domain.Entities;
using RepairFlow.Api.Mapping;

namespace RepairFlow.Api.Services;

/// <summary>Готовый к отдаче файл: поток, тип и исходное имя.</summary>
public sealed record AttachmentContent(Stream Stream, string ContentType, string FileName);

public interface IAttachmentService
{
    Task<IReadOnlyList<AttachmentDto>> GetAsync(Guid orderId, CancellationToken ct = default);

    Task<AttachmentDto> UploadAsync(Guid orderId, IFormFile file, CancellationToken ct = default);

    Task<AttachmentContent> DownloadAsync(Guid attachmentId, CancellationToken ct = default);

    Task DeleteAsync(Guid attachmentId, CancellationToken ct = default);
}

public sealed class AttachmentService : IAttachmentService
{
    private readonly AppDbContext _db;
    private readonly IOrderAccessGuard _guard;
    private readonly IFileStorage _storage;
    private readonly ICurrentUser _currentUser;
    private readonly StorageOptions _options;

    public AttachmentService(
        AppDbContext db,
        IOrderAccessGuard guard,
        IFileStorage storage,
        ICurrentUser currentUser,
        IOptions<StorageOptions> options)
    {
        _db = db;
        _guard = guard;
        _storage = storage;
        _currentUser = currentUser;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<AttachmentDto>> GetAsync(Guid orderId, CancellationToken ct = default)
    {
        await _guard.LoadOrderAsync(orderId, OrderAccessRequirement.Read, ct);

        var attachments = await _db.Attachments.AsNoTracking()
            .Include(a => a.UploadedBy)
            .Where(a => a.OrderId == orderId)
            .OrderByDescending(a => a.UploadedAt)
            .ToListAsync(ct);

        return attachments.Select(a => a.ToDto(UrlFor(a.Id))).ToList();
    }

    public async Task<AttachmentDto> UploadAsync(Guid orderId, IFormFile file, CancellationToken ct = default)
    {
        var order = await _guard.LoadOrderAsync(orderId, OrderAccessRequirement.Write, ct);

        Validate(file);

        var count = await _db.Attachments.CountAsync(a => a.OrderId == orderId, ct);
        if (count >= _options.MaxAttachmentsPerOrder)
        {
            throw new ConflictException($"К заявке нельзя приложить больше {_options.MaxAttachmentsPerOrder} файлов.");
        }

        var extension = Path.GetExtension(file.FileName);

        await using var stream = file.OpenReadStream();
        var storedPath = await _storage.SaveAsync(stream, extension, ct);

        var uploader = await _db.Users.FirstAsync(u => u.Id == _currentUser.Id, ct);

        var attachment = new Attachment
        {
            OrderId = order.Id,
            FileName = Path.GetFileName(file.FileName),
            StoredPath = storedPath,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            UploadedById = uploader.Id,
            UploadedBy = uploader,
            UploadedAt = DateTime.UtcNow
        };

        _db.Attachments.Add(attachment);

        var tracked = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (tracked is not null)
        {
            tracked.UpdatedAt = attachment.UploadedAt;
        }

        await _db.SaveChangesAsync(ct);

        return attachment.ToDto(UrlFor(attachment.Id));
    }

    public async Task<AttachmentContent> DownloadAsync(Guid attachmentId, CancellationToken ct = default)
    {
        var attachment = await _db.Attachments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId, ct)
            ?? throw NotFoundException.For("Файл", attachmentId);

        // Права проверяются по заявке-владельцу: прямая ссылка на файл ничего не открывает сама по себе.
        await _guard.LoadOrderAsync(attachment.OrderId, OrderAccessRequirement.Read, ct);

        if (!_storage.Exists(attachment.StoredPath))
        {
            throw new NotFoundException("Файл отсутствует в хранилище.");
        }

        return new AttachmentContent(
            _storage.OpenRead(attachment.StoredPath),
            attachment.ContentType,
            attachment.FileName);
    }

    public async Task DeleteAsync(Guid attachmentId, CancellationToken ct = default)
    {
        var attachment = await _db.Attachments.FirstOrDefaultAsync(a => a.Id == attachmentId, ct)
                         ?? throw NotFoundException.For("Файл", attachmentId);

        await _guard.LoadOrderAsync(attachment.OrderId, OrderAccessRequirement.Write, ct);

        if (!_currentUser.IsManager && attachment.UploadedById != _currentUser.Id)
        {
            throw new ForbiddenException("Удалить файл может тот, кто его загрузил, или менеджер.");
        }

        _db.Attachments.Remove(attachment);
        await _db.SaveChangesAsync(ct);

        _storage.Delete(attachment.StoredPath);
    }

    private void Validate(IFormFile file)
    {
        var errors = new Dictionary<string, string[]>();

        if (file.Length <= 0)
        {
            errors["file"] = new[] { "Файл пустой." };
        }
        else if (file.Length > _options.MaxFileSizeBytes)
        {
            var limitMb = _options.MaxFileSizeBytes / 1024d / 1024d;
            errors["file"] = new[] { $"Файл больше допустимых {limitMb:0.#} МБ." };
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!_options.AllowedExtensions.Contains(extension))
        {
            errors["fileName"] = new[]
            {
                $"Недопустимое расширение «{extension}». Разрешены: {string.Join(", ", _options.AllowedExtensions)}."
            };
        }

        if (!_options.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            errors["contentType"] = new[]
            {
                $"Недопустимый тип файла «{file.ContentType}»."
            };
        }

        if (errors.Count > 0)
        {
            throw new ValidationFailedException(errors);
        }
    }

    private static string UrlFor(Guid attachmentId) => $"/api/attachments/{attachmentId}";
}
