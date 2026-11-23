using Ordering.Application.Orders.CreateOrder;

namespace Ordering.Tests;

/// <summary>Huella del cuerpo para la idempotencia (ADR-0010).</summary>
public sealed class CreateOrderFingerprintTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static CreateOrderCommand Command(params CreateOrderLine[] lines) => new(CustomerId, lines);

    [Fact]
    public void Decimal_scale_does_not_change_the_fingerprint()
    {
        var a = Command(new CreateOrderLine("SKU-1", 2, 10m));
        var b = Command(new CreateOrderLine("SKU-1", 2, 10.00m));

        Assert.Equal(a.ComputeFingerprint(), b.ComputeFingerprint());
    }

    [Fact]
    public void Line_order_changes_the_fingerprint()
    {
        var a = Command(new CreateOrderLine("SKU-1", 1, 1m), new CreateOrderLine("SKU-2", 1, 1m));
        var b = Command(new CreateOrderLine("SKU-2", 1, 1m), new CreateOrderLine("SKU-1", 1, 1m));

        Assert.NotEqual(a.ComputeFingerprint(), b.ComputeFingerprint());
    }

    [Fact]
    public void Key_and_subject_are_not_part_of_the_fingerprint()
    {
        var a = Command(new CreateOrderLine("SKU-1", 1, 1m));
        var b = a with { IdempotencyKey = "k", Subject = "s" };

        Assert.Equal(a.ComputeFingerprint(), b.ComputeFingerprint());
    }

    [Fact]
    public void Different_price_changes_the_fingerprint()
    {
        var a = Command(new CreateOrderLine("SKU-1", 1, 10m));
        var b = Command(new CreateOrderLine("SKU-1", 1, 10.01m));

        Assert.NotEqual(a.ComputeFingerprint(), b.ComputeFingerprint());
    }
}
