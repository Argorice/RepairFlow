using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairFlow.Api.Authorization;
using RepairFlow.Api.Contracts;
using RepairFlow.Api.Domain.Enums;
using RepairFlow.Api.Services;

namespace RepairFlow.Api.Controllers;

/// <summary>Пользователи: справочник для менеджера и собственный профиль для всех.</summary>
[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    /// <summary>Список пользователей с фильтром по роли и поиском по имени и почте.</summary>
    [HttpGet]
    [Authorize(Policy = AppPolicies.ManagerOnly)]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetList(
        [FromQuery] UserRole? role,
        [FromQuery] string? search,
        CancellationToken ct) =>
        Ok(await _users.GetListAsync(role, search, ct));

    /// <summary>Завести сотрудника: мастера или менеджера.</summary>
    [HttpPost]
    [Authorize(Policy = AppPolicies.ManagerOnly)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken ct) =>
        Ok(await _users.CreateAsync(request, ct));

    /// <summary>Изменить роль, активность или контакты пользователя.</summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = AppPolicies.ManagerOnly)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Update(Guid id, UpdateUserRequest request, CancellationToken ct) =>
        Ok(await _users.UpdateAsync(id, request, ct));

    /// <summary>Обновить собственный профиль.</summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> UpdateProfile(UpdateProfileRequest request, CancellationToken ct) =>
        Ok(await _users.UpdateProfileAsync(request, ct));

    /// <summary>Сменить пароль. Все прочие сессии при этом завершаются.</summary>
    [HttpPost("me/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await _users.ChangePasswordAsync(request, ct);
        return NoContent();
    }
}
