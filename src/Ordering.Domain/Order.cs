namespace Ordering.Domain;

public sealed class Order
{
    public const int MaxLines = 50;

    private readonly List<OrderLine> _lines = new();

    // Constructor para EF Core. Los pedidos se crean con Order.Create.
    private Order()
    {
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public OrderStatus Status { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public decimal Total => _lines.Sum(l => l.Subtotal);

    public static Order Create(Guid customerId, DateTime createdAtUtc)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("CustomerId is required.");
        }

        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CreatedAtUtc = createdAtUtc,
            Status = OrderStatus.Pending
        };
    }

    public void AddLine(string sku, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("Sku is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("UnitPrice cannot be negative.");
        }

        if (_lines.Count >= MaxLines)
        {
            throw new DomainException($"An order cannot have more than {MaxLines} lines.");
        }

        _lines.Add(OrderLine.Create(sku.Trim(), quantity, unitPrice));
    }
}
