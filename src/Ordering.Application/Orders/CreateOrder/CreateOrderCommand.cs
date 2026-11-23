using MediatR;
using Ordering.Application.Abstractions;
using Ordering.Domain;

namespace Ordering.Application.Orders.CreateOrder;

public sealed record CreateOrderLine(string Sku, int Quantity, decimal UnitPrice);

public sealed record CreateOrderCommand(Guid CustomerId, IReadOnlyList<CreateOrderLine> Lines) : IRequest<OrderDto>;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly TimeProvider _timeProvider;

    public CreateOrderCommandHandler(IOrderRepository orders, TimeProvider timeProvider)
    {
        _orders = orders;
        _timeProvider = timeProvider;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Las invariantes las garantiza el dominio: si se violan, lanza DomainException (→ 400).
        var order = Order.Create(request.CustomerId, _timeProvider.GetUtcNow().UtcDateTime);

        foreach (var line in request.Lines)
        {
            order.AddLine(line.Sku, line.Quantity, line.UnitPrice);
        }

        await _orders.AddAsync(order, cancellationToken);
        await _orders.SaveChangesAsync(cancellationToken);

        return OrderDto.From(order);
    }
}
