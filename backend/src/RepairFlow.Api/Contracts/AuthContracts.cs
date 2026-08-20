using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Contracts;

/// <summary>Регистрация. Через этот метод создаётся только клиент — сотрудников заводит менеджер.</summary>
public sealed record RegisterRequest(string Email, string Password, string FullName, string? Phone);

public sealed record LoginRequest(string Email, string Password);

/// <summary>Вход в демо-режиме одной кнопкой: заказчик не будет ничего регистрировать.</summary>
public sealed record DemoLoginRequest(UserRole Role);

/// <summary>
/// Ответ авторизации. Refresh-токен сюда намеренно не попадает —
/// он уходит в httpOnly-куке и недоступен из JavaScript.
/// </summary>
public sealed record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAt, UserDto User);

public sealed record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    UserRole Role,
    string RoleLabel,
    bool IsActive,
    DateTime CreatedAt);

/// <summary>Краткая карточка пользователя для вложенных объектов.</summary>
public sealed record UserSummaryDto(Guid Id, string FullName, string Email, UserRole Role);
