using Ordering.Domain;

namespace Ordering.Tests;

/// <summary>Tests unitarios de referencia sobre el dominio.</summary>
public sealed class OrderTests
{
    [Fact]
    public void Total_is_the_sum_of_line_subtotals()
    {
        var order = Order.Create(Guid.NewGuid(), DateTime.UtcNow);

        order.AddLine("A", 2, 10m);
        order.AddLine("B", 1, 5.5m);

        Assert.Equal(25.5m, order.Total);
    }

    [Fact]
    public void Create_with_empty_customer_throws()
    {
        Assert.Throws<DomainException>(() => Order.Create(Guid.Empty, DateTime.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void AddLine_with_non_positive_quantity_throws(int quantity)
    {
        var order = Order.Create(Guid.NewGuid(), DateTime.UtcNow);

        Assert.Throws<DomainException>(() => order.AddLine("A", quantity, 1m));
    }
}
