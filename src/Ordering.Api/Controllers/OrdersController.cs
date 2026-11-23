using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ordering.Api.Auth;
using Ordering.Api.Contracts;
using Ordering.Application.Orders;
using Ordering.Application.Orders.CreateOrder;
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

    /// <summary>Crea un pedido. La validación de forma la hace [ApiController] con las DataAnnotations del request.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.OrdersWrite)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    // ProblemDetails y no ValidationProblemDetails: el 400 del dominio (DomainExceptionHandler) no trae "errors" (ADR-0001).
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        // Los '!' son seguros: [Required] ya garantizó que no son null antes de llegar aquí.
        var command = new CreateOrderCommand(
            request.CustomerId!.Value,
            request.Lines!.Select(l => new CreateOrderLine(l.Sku!, l.Quantity, l.UnitPrice!.Value)).ToList());

        var order = await _sender.Send(command, cancellationToken);
        return CreatedAtRoute("GetOrderById", new { id = order.Id }, order);
    }
}
