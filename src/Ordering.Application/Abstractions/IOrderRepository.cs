using Ordering.Domain;

namespace Ordering.Application.Abstractions;

/// <summary>Acceso a escritura: devuelve entidades con seguimiento (tracking) de EF Core.</summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Order order, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<IdempotencyRecord?> FindIdempotencyRecordAsync(string subject, string key, CancellationToken cancellationToken);

    Task AddIdempotencyRecordAsync(IdempotencyRecord record, CancellationToken cancellationToken);

    /// <summary>
    /// Como SaveChangesAsync, pero devuelve false si otra petición ya guardó la misma clave de idempotencia
    /// (carrera, ADR-0011). En ese caso no se guarda nada y se descartan los cambios pendientes.
    /// </summary>
    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken);
}
