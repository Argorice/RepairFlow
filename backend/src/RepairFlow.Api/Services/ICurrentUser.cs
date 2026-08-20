using System.Security.Claims;
using RepairFlow.Api.Common;
using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Services;

/// <summary>Текущий пользователь запроса. Позволяет сервисам не зависеть от HttpContext напрямую.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid Id { get; }

    string Email { get; }

    UserRole Role { get; }

    bool IsManager { get; }

    bool IsTechnician { get; }

    bool IsClient { get; }

    string? IpAddress { get; }
}

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid Id
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id)
                ? id
                : throw new UnauthorizedException("В токене отсутствует идентификатор пользователя.");
        }
    }

    public string Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public UserRole Role
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(raw, out var role)
                ? role
                : throw new UnauthorizedException("В токене отсутствует роль пользователя.");
        }
    }

    public bool IsManager => Role == UserRole.Manager;

    public bool IsTechnician => Role == UserRole.Technician;

    public bool IsClient => Role == UserRole.Client;

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
