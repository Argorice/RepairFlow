using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairFlow.Api.Domain.Entities;

namespace RepairFlow.Api.Data.Configurations;

public sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("order_status_history");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.FromStatus)
            .HasMaxLength(40)
            .HasConversion<string>();

        builder.Property(h => h.ToStatus)
            .IsRequired()
            .HasMaxLength(40)
            .HasConversion<string>();

        builder.Property(h => h.Comment)
            .HasMaxLength(1000);

        builder.HasIndex(h => new { h.OrderId, h.ChangedAt });

        builder.HasOne(h => h.ChangedBy)
            .WithMany()
            .HasForeignKey(h => h.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
