using RepairFlow.Api.Domain.Enums;

namespace RepairFlow.Api.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public UserRole Role { get; set; } = UserRole.Client;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    /// <summary>Заявки, созданные пользователем как клиентом.</summary>
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    /// <summary>Заявки, назначенные пользователю как мастеру.</summary>
    public ICollection<Order> AssignedOrders { get; set; } = new List<Order>();
}
