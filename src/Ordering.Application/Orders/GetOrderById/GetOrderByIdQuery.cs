using MediatR;
using Ordering.Application.Abstractions;

namespace Ordering.Application.Orders.GetOrderById;

public sealed record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto?>;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly IOrderReadService _orders;

    public GetOrderByIdQueryHandler(IOrderReadService orders)
    {
        _orders = orders;
    }

    public Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken) =>
        _orders.GetByIdAsync(request.Id, cancellationToken);
}
