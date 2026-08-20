using Microsoft.EntityFrameworkCore;
using RepairFlow.Api.Domain.Entities;

namespace RepairFlow.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<Attachment> Attachments => Set<Attachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
