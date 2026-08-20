using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairFlow.Api.Contracts;
using RepairFlow.Api.Services;

namespace RepairFlow.Api.Controllers;

/// <summary>Файлы заявки: фото проблемы, чеки, акты.</summary>
[ApiController]
[Route("api")]
[Authorize]
public sealed class AttachmentsController : ControllerBase
{
    private readonly IAttachmentService _attachments;

    public AttachmentsController(IAttachmentService attachments) => _attachments = attachments;

    [HttpGet("orders/{orderId:guid}/attachments")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IReadOnlyList<AttachmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AttachmentDto>>> Get(Guid orderId, CancellationToken ct) =>
        Ok(await _attachments.GetAsync(orderId, ct));

    /// <summary>Загрузка файла: не больше 10 МБ, только изображения и PDF.</summary>
    [HttpPost("orders/{orderId:guid}/attachments")]
    [Produces("application/json")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [ProducesResponseType(typeof(AttachmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AttachmentDto>> Upload(
        Guid orderId,
        IFormFile file,
        CancellationToken ct) =>
        Ok(await _attachments.UploadAsync(orderId, file, ct));

    /// <summary>Отдача файла с проверкой прав по заявке-владельцу.</summary>
    [HttpGet("attachments/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, [FromQuery] bool download, CancellationToken ct)
    {
        var content = await _attachments.DownloadAsync(id, ct);

        return download
            ? File(content.Stream, content.ContentType, content.FileName)
            : File(content.Stream, content.ContentType);
    }

    [HttpDelete("attachments/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _attachments.DeleteAsync(id, ct);
        return NoContent();
    }
}
