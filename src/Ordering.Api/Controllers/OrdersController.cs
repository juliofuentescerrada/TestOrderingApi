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

    /// <summary>
    /// Crea un pedido. La validación de forma la hace [ApiController] con las DataAnnotations del request.
    /// Con la cabecera Idempotency-Key, repetir la petición devuelve el mismo pedido (ADR-0007 a ADR-0012).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.OrdersWrite)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    // ProblemDetails y no ValidationProblemDetails: el 400 del dominio (DomainExceptionHandler) no trae "errors" (ADR-0001).
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderDto>> Create(
        CreateOrderRequest request,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        string? subject = null;

        // El binding convierte una cabecera vacía en null (ConvertEmptyStringToNull), así que se mira la cabecera cruda:
        // si no, "Idempotency-Key:" vacía se trataría como ausente y el pedido se crearía sin idempotencia (ADR-0012).
        if (Request.Headers.ContainsKey(IdempotencyKeyHeader))
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > MaxIdempotencyKeyLength)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Idempotency-Key header.",
                    detail: $"The {IdempotencyKeyHeader} header must contain between 1 and {MaxIdempotencyKeyLength} non-blank characters.");
            }

            // La clave es por cliente (ADR-0008): sin sub no hay espacio de claves propio.
            subject = User.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(subject))
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Idempotency requires an identified client.",
                    detail: $"The access token has no 'sub' claim, which is required to use the {IdempotencyKeyHeader} header.");
            }
        }

        // Los '!' son seguros: [Required] ya garantizó que no son null antes de llegar aquí.
        var command = new CreateOrderCommand(
            request.CustomerId!.Value,
            request.Lines!.Select(l => new CreateOrderLine(l.Sku!, l.Quantity, l.UnitPrice!.Value)).ToList(),
            subject is null ? null : idempotencyKey,
            subject);

        var result = await _sender.Send(command, cancellationToken);

        switch (result.Outcome)
        {
            case CreateOrderOutcome.KeyReusedWithDifferentBody:
                return Problem(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Idempotency key already used with a different request.",
                    detail: $"The {IdempotencyKeyHeader} was already used to create an order with a different body. Use a new key for a different order.");

            case CreateOrderOutcome.Replayed:
                // La respuesta es la misma que la original; la cabecera solo ayuda a depurar (ADR-0009).
                Response.Headers[IdempotentReplayedHeader] = "true";
                break;
        }

        var order = result.Order!;
        return CreatedAtRoute("GetOrderById", new { id = order.Id }, order);
    }

    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private const string IdempotentReplayedHeader = "Idempotent-Replayed";
    private const int MaxIdempotencyKeyLength = 64;
}
