using Ordering.Domain;

namespace Ordering.Application.Abstractions;

/// <summary>Acceso a escritura: devuelve entidades con seguimiento (tracking) de EF Core.</summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Order order, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
