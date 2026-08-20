using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RepairFlow.Api.Configuration;
using RepairFlow.Api.Contracts;
using RepairFlow.Api.Services;

namespace RepairFlow.Api.Controllers;

/// <summary>Регистрация, вход, обновление токена и выход.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ICurrentUser _currentUser;
    private readonly JwtOptions _jwt;

    public AuthController(IAuthService auth, ICurrentUser currentUser, IOptions<JwtOptions> jwt)
    {
        _auth = auth;
        _currentUser = currentUser;
        _jwt = jwt.Value;
    }

    /// <summary>Регистрация клиента. Сотрудников заводит менеджер через раздел пользователей.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(request, _currentUser.IpAddress, ct);
        return Issue(result);
    }

    /// <summary>Вход по почте и паролю. Access-токен возвращается в теле, refresh — в httpOnly-куке.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request, _currentUser.IpAddress, ct);
        return Issue(result);
    }

    /// <summary>Вход в демо-аккаунт одной кнопкой: клиент, мастер или менеджер.</summary>
    [HttpPost("demo")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuthResponse>> Demo(DemoLoginRequest request, CancellationToken ct)
    {
        var result = await _auth.DemoLoginAsync(request.Role, _currentUser.IpAddress, ct);
        return Issue(result);
    }

    /// <summary>Обновление пары токенов по refresh-куке. Старый refresh-токен при этом отзывается.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken ct)
    {
        var token = Request.Cookies[_jwt.RefreshCookieName];
        var result = await _auth.RefreshAsync(token, _currentUser.IpAddress, ct);
        return Issue(result);
    }

    /// <summary>Выход: refresh-токен отзывается, кука удаляется.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var token = Request.Cookies[_jwt.RefreshCookieName];
        await _auth.LogoutAsync(token, ct);

        Response.Cookies.Delete(_jwt.RefreshCookieName, BuildCookieOptions(null));

        return NoContent();
    }

    /// <summary>Текущий пользователь по access-токену.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct) =>
        Ok(await _auth.GetCurrentAsync(_currentUser.Id, ct));

    private ActionResult<AuthResponse> Issue(AuthResult result)
    {
        Response.Cookies.Append(
            _jwt.RefreshCookieName,
            result.RefreshToken,
            BuildCookieOptions(result.RefreshExpiresAt));

        return Ok(result.Response);
    }

    private CookieOptions BuildCookieOptions(DateTime? expiresAt) => new()
    {
        HttpOnly = true,
        // SameSite=None обязателен, когда фронт живёт на другом домене; он же требует Secure.
        SameSite = _jwt.CrossSiteCookies ? SameSiteMode.None : SameSiteMode.Lax,
        Secure = _jwt.CrossSiteCookies || Request.IsHttps,
        Path = "/api/auth",
        Expires = expiresAt
    };
}
