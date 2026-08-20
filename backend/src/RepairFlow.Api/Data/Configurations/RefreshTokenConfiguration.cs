using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairFlow.Api.Domain.Entities;

namespace RepairFlow.Api.Data.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.CreatedByIp)
            .HasMaxLength(64);

        builder.Property(t => t.ReplacedByToken)
            .HasMaxLength(128);

        builder.HasIndex(t => t.Token).IsUnique();

        builder.HasIndex(t => new { t.UserId, t.ExpiresAt });

        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.IsActive);

        builder.HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
