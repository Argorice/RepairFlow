using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairFlow.Api.Authorization;
using RepairFlow.Api.Contracts;
using RepairFlow.Api.Services;

namespace RepairFlow.Api.Controllers;

/// <summary>Смета заявки: позиции и согласование клиентом.</summary>
[ApiController]
[Route("api/orders/{orderId:guid}")]
[Authorize]
[Produces("application/json")]
public sealed class OrderItemsController : ControllerBase
{
    private readonly IOrderItemService _items;

    public OrderItemsController(IOrderItemService items) => _items = items;

    /// <summary>Смета с подсчитанными итогами по запчастям и работам.</summary>
    [HttpGet("items")]
    [ProducesResponseType(typeof(EstimateDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EstimateDto>> Get(Guid orderId, CancellationToken ct) =>
        Ok(await _items.GetAsync(orderId, ct));

    /// <summary>Добавить позицию в смету. Доступно мастеру и менеджеру.</summary>
    [HttpPost("items")]
    [Authorize(Policy = AppPolicies.StaffOnly)]
    [ProducesResponseType(typeof(OrderItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderItemDto>> Add(
        Guid orderId,
        SaveOrderItemRequest request,
        CancellationToken ct) =>
        Ok(await _items.AddAsync(orderId, request, ct));

    /// <summary>Изменить позицию сметы.</summary>
    [HttpPut("items/{itemId:guid}")]
    [Authorize(Policy = AppPolicies.StaffOnly)]
    [ProducesResponseType(typeof(OrderItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderItemDto>> Update(
        Guid orderId,
        Guid itemId,
        SaveOrderItemRequest request,
        CancellationToken ct) =>
        Ok(await _items.UpdateAsync(orderId, itemId, request, ct));

    /// <summary>Удалить позицию сметы.</summary>
    [HttpDelete("items/{itemId:guid}")]
    [Authorize(Policy = AppPolicies.StaffOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid orderId, Guid itemId, CancellationToken ct)
    {
        await _items.DeleteAsync(orderId, itemId, ct);
        return NoContent();
    }

    /// <summary>Клиент подтверждает смету — заявка уходит в работу.</summary>
    [HttpPost("estimate/approve")]
    [Authorize(Policy = AppPolicies.ClientOnly)]
    [ProducesResponseType(typeof(OrderDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDetailsDto>> Approve(Guid orderId, CancellationToken ct) =>
        Ok(await _items.ApproveAsync(orderId, ct));

    /// <summary>Клиент отказывается от ремонта.</summary>
    [HttpPost("estimate/reject")]
    [Authorize(Policy = AppPolicies.ClientOnly)]
    [ProducesResponseType(typeof(OrderDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDetailsDto>> Reject(
        Guid orderId,
        RejectEstimateRequest request,
        CancellationToken ct) =>
        Ok(await _items.RejectAsync(orderId, request, ct));
}
