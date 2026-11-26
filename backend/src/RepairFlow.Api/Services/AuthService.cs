using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RepairFlow.Api.Common;
using RepairFlow.Api.Configuration;
using RepairFlow.Api.Contracts;
using RepairFlow.Api.Data;
using RepairFlow.Api.Domain.Entities;
using RepairFlow.Api.Domain.Enums;
using RepairFlow.Api.Mapping;
using RepairFlow.Api.Localization;

namespace RepairFlow.Api.Services;

/// <summary>Результат аутентификации: тело ответа плюс refresh-токен, который контроллер положит в куку.</summary>
public sealed record AuthResult(AuthResponse Response, string RefreshToken, DateTime RefreshExpiresAt);

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, string? ip, CancellationToken ct = default);

    Task<AuthResult> LoginAsync(LoginRequest request, string? ip, CancellationToken ct = default);

    Task<AuthResult> DemoLoginAsync(UserRole role, string? ip, CancellationToken ct = default);

    Task<AuthResult> RefreshAsync(string? refreshToken, string? ip, CancellationToken ct = default);

    Task LogoutAsync(string? refreshToken, CancellationToken ct = default);

    Task<UserDto> GetCurrentAsync(Guid userId, CancellationToken ct = default);
}

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokens;
    private readonly DemoOptions _demo;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext db,
        ITokenService tokens,
        IOptions<DemoOptions> demo,
        ILogger<AuthService> logger)
    {
        _db = db;
        _tokens = tokens;
        _demo = demo.Value;
        _logger = logger;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, string? ip, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
        {
            throw new ConflictException(Texts.Pick("An account with this email is already registered.", "Пользователь с такой почтой уже зарегистрирован."));
        }

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            // Через публичную регистрацию заводится только клиент: роль из запроса не принимается вообще.
            Role = UserRole.Client,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User registered: {Email}", user.Email);

        return await IssueAsync(user, ip, ct);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, string? ip, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Одна и та же формулировка на «нет пользователя» и «неверный пароль» — чтобы почты нельзя было перебрать.
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException(Texts.Pick("Wrong email or password.", "Неверная почта или пароль."));
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException(Texts.Pick("This account is disabled. Contact a manager.", "Учётная запись отключена. Обратитесь к менеджеру."));
        }

        return await IssueAsync(user, ip, ct);
    }

    public async Task<AuthResult> DemoLoginAsync(UserRole role, string? ip, CancellationToken ct = default)
    {
        if (!_demo.SeedOnStartup)
        {
            throw new ForbiddenException(Texts.Pick("Demo mode is turned off on this instance.", "Демо-режим отключён на этом стенде."));
        }

        var email = DbSeeder.DemoEmailFor(role);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct)
                   ?? throw new NotFoundException(Texts.Pick("Demo account not found. Make sure the database has been seeded.", "Демо-аккаунт не найден. Проверьте, что база заполнена демо-данными."));

        return await IssueAsync(user, ip, ct);
    }

    public async Task<AuthResult> RefreshAsync(string? refreshToken, string? ip, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedException(Texts.Pick("The refresh token is missing.", "Refresh-токен отсутствует."));
        }

        var stored = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken, ct);

        if (stored is null)
        {
            throw new UnauthorizedException(Texts.Pick("The refresh token is invalid.", "Refresh-токен недействителен."));
        }

        if (!stored.IsActive)
        {
            // Повторное использование отозванного токена — признак кражи: гасим всю сессию пользователя.
            await RevokeAllAsync(stored.UserId, ct);
            _logger.LogWarning("Reuse of a revoked refresh token by user {UserId}", stored.UserId);
            throw new UnauthorizedException(Texts.Pick("Your session has ended. Please sign in again.", "Сессия завершена. Войдите заново."));
        }

        if (!stored.User.IsActive)
        {
            throw new ForbiddenException(Texts.Pick("This account is disabled.", "Учётная запись отключена."));
        }

        var result = await IssueAsync(stored.User, ip, ct, rotatedFrom: stored);
        return result;
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken, ct);
        if (stored is null || !stored.IsActive)
        {
            return;
        }

        stored.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<UserDto> GetCurrentAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new UnauthorizedException(Texts.Pick("User not found.", "Пользователь не найден."));

        return user.ToDto();
    }

    private async Task<AuthResult> IssueAsync(
        User user,
        string? ip,
        CancellationToken ct,
        RefreshToken? rotatedFrom = null)
    {
        var refreshValue = _tokens.CreateRefreshToken();
        var expiresAt = _tokens.RefreshTokenExpiry();

        if (rotatedFrom is not null)
        {
            rotatedFrom.RevokedAt = DateTime.UtcNow;
            rotatedFrom.ReplacedByToken = refreshValue;
        }

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshValue,
            ExpiresAt = expiresAt,
            CreatedByIp = ip
        });

        await _db.SaveChangesAsync(ct);

        var access = _tokens.CreateAccessToken(user);
        var response = new AuthResponse(access.Value, access.ExpiresAt, user.ToDto());

        return new AuthResult(response, refreshValue, expiresAt);
    }

    private async Task RevokeAllAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync(ct);

        foreach (var token in active)
        {
            token.RevokedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
