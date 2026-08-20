using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairFlow.Api.Contracts;
using RepairFlow.Api.Services;

namespace RepairFlow.Api.Controllers;

/// <summary>Переписка по заявке. Внутренние заметки клиенту не отдаются.</summary>
[ApiController]
[Route("api/orders/{orderId:guid}/comments")]
[Authorize]
[Produces("application/json")]
public sealed class CommentsController : ControllerBase
{
    private readonly ICommentService _comments;

    public CommentsController(ICommentService comments) => _comments = comments;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CommentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> Get(Guid orderId, CancellationToken ct) =>
        Ok(await _comments.GetAsync(orderId, ct));

    /// <summary>Добавить комментарий. Флаг IsInternal учитывается только для мастера и менеджера.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CommentDto>> Add(
        Guid orderId,
        CreateCommentRequest request,
        CancellationToken ct) =>
        Ok(await _comments.AddAsync(orderId, request, ct));
}
