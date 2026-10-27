using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ordering.Api.Auth;
using Ordering.Application.Orders;
using Ordering.Application.Orders.GetOrderById;

namespace Ordering.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Endpoint de referencia: lectura protegida con la política orders:read.</summary>
    [HttpGet("{id:guid}", Name = "GetOrderById")]
    [Authorize(Policy = Policies.OrdersRead)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _sender.Send(new GetOrderByIdQuery(id), cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }
}
