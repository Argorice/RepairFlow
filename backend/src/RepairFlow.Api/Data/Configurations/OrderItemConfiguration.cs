using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairFlow.Api.Domain.Entities;

namespace RepairFlow.Api.Data.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Type)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Quantity).HasPrecision(10, 2);
        builder.Property(i => i.UnitPrice).HasPrecision(12, 2);

        builder.HasIndex(i => i.OrderId);
    }
}
