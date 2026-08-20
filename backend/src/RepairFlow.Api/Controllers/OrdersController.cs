using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairFlow.Api.Authorization;
using RepairFlow.Api.Common;
using RepairFlow.Api.Contracts;
using RepairFlow.Api.Services;

namespace RepairFlow.Api.Controllers;

/// <summary>Заявки на ремонт. Видимость записей ограничивается ролью на уровне запроса к базе.</summary>
[ApiController]
[Route("api/orders")]
[Authorize]
[Produces("application/json")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    /// <summary>
    /// Список заявок с фильтрами, поиском, сортировкой и пагинацией.
    /// Клиент видит свои, мастер — назначенные ему и свободные, менеджер — все.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderListItemDto>>> GetList(
        [FromQuery] OrderQuery query,
        CancellationToken ct) =>
        Ok(await _orders.GetListAsync(query, ct));

    /// <summary>Карточка заявки вместе с доступными текущему пользователю переходами статуса.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDetailsDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await _orders.GetByIdAsync(id, ct));

    /// <summary>Создание заявки клиентом. Номер вида RF-2026-0001 выдаётся сервером.</summary>
    [HttpPost]
    [Authorize(Policy = AppPolicies.ClientOnly)]
    [ProducesResponseType(typeof(OrderDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDetailsDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var created = await _orders.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Частичное редактирование заявки. Передавайте только изменяемые поля.</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(OrderDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDetailsDto>> Update(
        Guid id,
        UpdateOrderRequest request,
        CancellationToken ct) =>
        Ok(await _orders.UpdateAsync(id, request, ct));

    /// <summary>Смена статуса с проверкой допустимости перехода для роли пользователя.</summary>
    [HttpPost("{id:guid}/status")]
    [ProducesResponseType(typeof(OrderDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDetailsDto>> ChangeStatus(
        Guid id,
        ChangeStatusRequest request,
        CancellationToken ct) =>
        Ok(await _orders.ChangeStatusAsync(id, request, ct));

    /// <summary>Назначение мастера. Передайте null, чтобы снять назначение.</summary>
    [HttpPost("{id:guid}/assign")]
    [Authorize(Policy = AppPolicies.ManagerOnly)]
    [ProducesResponseType(typeof(OrderDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrderDetailsDto>> Assign(
        Guid id,
        AssignTechnicianRequest request,
        CancellationToken ct) =>
        Ok(await _orders.AssignAsync(id, request, ct));

    /// <summary>История переходов статуса: кто, когда и с каким комментарием.</summary>
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderStatusHistoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderStatusHistoryDto>>> GetHistory(Guid id, CancellationToken ct) =>
        Ok(await _orders.GetHistoryAsync(id, ct));
}
