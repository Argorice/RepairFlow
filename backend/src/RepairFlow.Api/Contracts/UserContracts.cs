using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Contracts;

/// <summary>Менеджер меняет роль и активность. Пароль и почта здесь не трогаются намеренно.</summary>
public sealed record UpdateUserRequest(UserRole? Role, bool? IsActive, string? FullName, string? Phone);

public sealed record CreateUserRequest(
    string Email,
    string Password,
    string FullName,
    string? Phone,
    UserRole Role);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record UpdateProfileRequest(string FullName, string? Phone);
