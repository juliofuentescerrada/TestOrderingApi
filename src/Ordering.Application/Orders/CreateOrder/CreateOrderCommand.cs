using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Ordering.Application.Abstractions;
using Ordering.Domain;

namespace Ordering.Application.Orders.CreateOrder;

public sealed record CreateOrderLine(string Sku, int Quantity, decimal UnitPrice);

/// <param name="IdempotencyKey">Opcional. Si viene, Subject también es obligatorio (la clave es por cliente, ADR-0008).</param>
public sealed record CreateOrderCommand(
    Guid CustomerId,
    IReadOnlyList<CreateOrderLine> Lines,
    string? IdempotencyKey = null,
    string? Subject = null) : IRequest<CreateOrderResult>
{
    /// <summary>Huella del contenido del pedido (ADR-0010). No incluye la clave ni el sujeto.</summary>
    public string ComputeFingerprint()
    {
        var payload = new
        {
            CustomerId,
            Lines = Lines.Select(l => new { l.Sku, l.Quantity, UnitPrice = NormalizeScale(l.UnitPrice) })
        };

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        return Convert.ToHexString(hash);
    }

    // decimal conserva la escala: 10.00m se serializa como "10.00" y 10m como "10", y darían huellas distintas.
    // Dividir por 1.000…0m elimina los ceros finales sin cambiar el valor.
    private static decimal NormalizeScale(decimal value) => value / 1.0000000000000000000000000000m;
}

public enum CreateOrderOutcome
{
    Created,
    Replayed,
    KeyReusedWithDifferentBody
}

/// <summary>Resultado del caso de uso; el controller lo traduce a 201 / 201 + Idempotent-Replayed / 422 (ADR-0011).</summary>
public sealed record CreateOrderResult(CreateOrderOutcome Outcome, OrderDto? Order);

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    private readonly IOrderRepository _orders;
    private readonly TimeProvider _timeProvider;

    public CreateOrderCommandHandler(IOrderRepository orders, TimeProvider timeProvider)
    {
        _orders = orders;
        _timeProvider = timeProvider;
    }

    public async Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.IdempotencyKey is null)
        {
            var order = CreateOrder(request);
            await _orders.AddAsync(order, cancellationToken);
            await _orders.SaveChangesAsync(cancellationToken);
            return new CreateOrderResult(CreateOrderOutcome.Created, OrderDto.From(order));
        }

        var subject = request.Subject
            ?? throw new InvalidOperationException("Subject is required when an idempotency key is provided.");
        var fingerprint = request.ComputeFingerprint();

        var existing = await _orders.FindIdempotencyRecordAsync(subject, request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return await ResolveExistingAsync(existing, fingerprint, cancellationToken);
        }

        // Las invariantes las garantiza el dominio: si se violan, lanza DomainException (→ 400) y no se guarda la clave.
        var newOrder = CreateOrder(request);
        await _orders.AddAsync(newOrder, cancellationToken);
        await _orders.AddIdempotencyRecordAsync(
            new IdempotencyRecord(subject, request.IdempotencyKey, fingerprint, newOrder.Id, _timeProvider.GetUtcNow().UtcDateTime),
            cancellationToken);

        // Pedido y clave en un único SaveChanges: o se guardan ambos o ninguno (ADR-0011).
        if (await _orders.TrySaveChangesAsync(cancellationToken))
        {
            return new CreateOrderResult(CreateOrderOutcome.Created, OrderDto.From(newOrder));
        }

        // Otra petición con la misma clave ganó la carrera: se responde según lo que ella guardó.
        var winner = await _orders.FindIdempotencyRecordAsync(subject, request.IdempotencyKey, cancellationToken)
            ?? throw new InvalidOperationException("Idempotency record missing after a key conflict.");
        return await ResolveExistingAsync(winner, fingerprint, cancellationToken);
    }

    private Order CreateOrder(CreateOrderCommand request)
    {
        var order = Order.Create(request.CustomerId, _timeProvider.GetUtcNow().UtcDateTime);

        foreach (var line in request.Lines)
        {
            order.AddLine(line.Sku, line.Quantity, line.UnitPrice);
        }

        return order;
    }

    private async Task<CreateOrderResult> ResolveExistingAsync(IdempotencyRecord record, string fingerprint, CancellationToken cancellationToken)
    {
        if (record.RequestHash != fingerprint)
        {
            return new CreateOrderResult(CreateOrderOutcome.KeyReusedWithDifferentBody, null);
        }

        var order = await _orders.GetByIdAsync(record.OrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {record.OrderId} referenced by an idempotency record was not found.");
        return new CreateOrderResult(CreateOrderOutcome.Replayed, OrderDto.From(order));
    }
}
