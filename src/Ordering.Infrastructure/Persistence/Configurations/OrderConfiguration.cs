using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Application.Abstractions;
using Ordering.Domain;

namespace Ordering.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerId).IsRequired();
        builder.Property(o => o.CreatedAtUtc).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.Ignore(o => o.Total);

        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey("OrderId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(o => o.CustomerId);
    }
}

public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Sku).HasMaxLength(64).IsRequired();
        builder.Property(l => l.Quantity).IsRequired();
        builder.Property(l => l.UnitPrice).HasPrecision(18, 2).IsRequired();

        builder.Ignore(l => l.Subtotal);
    }
}

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        // La PK compuesta es la garantía frente a peticiones simultáneas con la misma clave (ADR-0011).
        builder.HasKey(r => new { r.Subject, r.Key });

        // SQLite no aplica estas longitudes; la de Key se valida en la API (ADR-0012).
        builder.Property(r => r.Subject).HasMaxLength(256);
        builder.Property(r => r.Key).HasMaxLength(64);
        builder.Property(r => r.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(r => r.OrderId).IsRequired();
        builder.Property(r => r.CreatedAtUtc).IsRequired();
    }
}
