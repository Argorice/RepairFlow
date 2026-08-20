using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairFlow.Api.Domain.Entities;

namespace RepairFlow.Api.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Phone)
            .HasMaxLength(32);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(32)
            .HasConversion<string>();

        // Уникальность по нормализованному e-mail: регистр не должен позволять завести дубль.
        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasMany(u => u.Orders)
            .WithOne(o => o.Client)
            .HasForeignKey(o => o.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.AssignedOrders)
            .WithOne(o => o.AssignedTechnician)
            .HasForeignKey(o => o.AssignedTechnicianId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
