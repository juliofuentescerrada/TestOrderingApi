namespace Ordering.Application.Abstractions;

/// <summary>
/// Registro de una clave de idempotencia ya usada (ADR-0011). Es un detalle de persistencia,
/// no del dominio: identifica la clave por cliente (ADR-0008) y apunta al pedido que creó.
/// </summary>
public sealed record IdempotencyRecord(string Subject, string Key, string RequestHash, Guid OrderId, DateTime CreatedAtUtc);
