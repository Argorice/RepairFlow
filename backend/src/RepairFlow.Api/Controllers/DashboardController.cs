using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairFlow.Api.Authorization;
using RepairFlow.Api.Contracts;
using RepairFlow.Api.Services;

namespace RepairFlow.Api.Controllers;

/// <summary>Аналитика для менеджера.</summary>
[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = AppPolicies.ManagerOnly)]
[Produces("application/json")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    /// <summary>
    /// Сводка: счётчики по статусам, выручка и средний срок ремонта за период,
    /// динамика по дням и загрузка мастеров. Период по умолчанию — последние 30 дней.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct) =>
        Ok(await _dashboard.GetSummaryAsync(from, to, ct));
}
