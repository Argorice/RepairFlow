using Microsoft.EntityFrameworkCore;
using RepairFlow.Api.Common;
using RepairFlow.Api.Contracts;
using RepairFlow.Api.Data;
using RepairFlow.Api.Domain.Entities;
using RepairFlow.Api.Domain.Enums;
using RepairFlow.Api.Mapping;

namespace RepairFlow.Api.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetListAsync(UserRole? role, string? search, CancellationToken ct = default);

    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default);

    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);

    Task<UserDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);

    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
}

public sealed class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UserService(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<UserDto>> GetListAsync(
        UserRole? role,
        string? search,
        CancellationToken ct = default)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (role is not null)
        {
            query = query.Where(u => u.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = "%" + search.Trim() + "%";
            query = query.Where(u =>
                EF.Functions.ILike(u.FullName, pattern) ||
                EF.Functions.ILike(u.Email, pattern));
        }

        var users = await query
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FullName)
            .ToListAsync(ct);

        return users.Select(u => u.ToDto()).ToList();
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
        {
            throw new ConflictException("Пользователь с такой почтой уже существует.");
        }

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Role = request.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return user.ToDto();
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
                   ?? throw NotFoundException.For("Пользователь", id);

        // Защита от выстрела в ногу: менеджер не может отобрать права у самого себя.
        if (user.Id == _currentUser.Id)
        {
            if (request.Role is not null && request.Role != user.Role)
            {
                throw new ConflictException("Нельзя сменить роль самому себе.");
            }

            if (request.IsActive == false)
            {
                throw new ConflictException("Нельзя отключить собственную учётную запись.");
            }
        }

        if (request.Role is not null && request.Role != user.Role)
        {
            await EnsureNotLastManagerAsync(user, request.Role.Value, ct);
            user.Role = request.Role.Value;
        }

        if (request.IsActive is not null)
        {
            if (!request.IsActive.Value && user.Role == UserRole.Manager)
            {
                await EnsureNotLastManagerAsync(user, UserRole.Client, ct);
            }

            user.IsActive = request.IsActive.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            user.FullName = request.FullName.Trim();
        }

        if (request.Phone is not null)
        {
            user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        }

        await _db.SaveChangesAsync(ct);

        return user.ToDto();
    }

    public async Task<UserDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.Id, ct)
                   ?? throw new UnauthorizedException("Пользователь не найден.");

        user.FullName = request.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();

        await _db.SaveChangesAsync(ct);

        return user.ToDto();
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.Id, ct)
                   ?? throw new UnauthorizedException("Пользователь не найден.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                ["currentPassword"] = new[] { "Текущий пароль указан неверно." }
            });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        // Смена пароля завершает все прочие сессии — иначе украденный refresh-токен продолжит работать.
        var now = DateTime.UtcNow;
        var tokens = await _db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsureNotLastManagerAsync(User user, UserRole newRole, CancellationToken ct)
    {
        if (user.Role != UserRole.Manager || newRole == UserRole.Manager)
        {
            return;
        }

        var managers = await _db.Users.CountAsync(u => u.Role == UserRole.Manager && u.IsActive, ct);
        if (managers <= 1)
        {
            throw new ConflictException("В системе должен остаться хотя бы один активный менеджер.");
        }
    }
}
