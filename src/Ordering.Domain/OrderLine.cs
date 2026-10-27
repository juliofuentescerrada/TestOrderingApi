namespace Ordering.Domain;

public sealed class OrderLine
{
    // Constructor para EF Core. Las líneas se crean desde Order.AddLine.
    private OrderLine()
    {
    }

    public Guid Id { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal Subtotal => Quantity * UnitPrice;

    internal static OrderLine Create(string sku, int quantity, decimal unitPrice) =>
        new()
        {
            Id = Guid.NewGuid(),
            Sku = sku,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
}
