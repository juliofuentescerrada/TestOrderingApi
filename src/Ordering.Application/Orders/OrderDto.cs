using Ordering.Domain;

namespace Ordering.Application.Orders;

public sealed record OrderLineDto(string Sku, int Quantity, decimal UnitPrice);

public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    DateTime CreatedAtUtc,
    string Status,
    decimal Total,
    IReadOnlyList<OrderLineDto> Lines)
{
    public static OrderDto From(Order order) =>
        new(
            order.Id,
            order.CustomerId,
            order.CreatedAtUtc,
            order.Status.ToString(),
            order.Total,
            order.Lines.Select(l => new OrderLineDto(l.Sku, l.Quantity, l.UnitPrice)).ToList());
}
