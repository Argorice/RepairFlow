using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepairFlow.Api.Domain.Entities;

namespace RepairFlow.Api.Data.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Number)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(o => o.DeviceType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.Brand)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.Model)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.SerialNumber)
            .HasMaxLength(100);

        builder.Property(o => o.ProblemDescription)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(40)
            .HasConversion<string>();

        builder.Property(o => o.Priority)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(o => o.EstimatedCost).HasPrecision(12, 2);
        builder.Property(o => o.FinalCost).HasPrecision(12, 2);

        builder.HasIndex(o => o.Number).IsUnique();
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.ClientId);
        builder.HasIndex(o => o.AssignedTechnicianId);
        builder.HasIndex(o => o.CreatedAt);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.History)
            .WithOne(h => h.Order)
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Comments)
            .WithOne(c => c.Order)
            .HasForeignKey(c => c.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Attachments)
            .WithOne(a => a.Order)
            .HasForeignKey(a => a.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
