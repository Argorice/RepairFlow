namespace RepairFlow.Api.Domain.Entities;

/// <summary>
/// Refresh-токен с ротацией: при обновлении текущий отзывается и в <see cref="ReplacedByToken"/>
/// записывается выданный на замену. Это позволяет обнаружить повторное использование украденного токена.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Token { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedByIp { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByToken { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsActive => RevokedAt is null && !IsExpired;
}
