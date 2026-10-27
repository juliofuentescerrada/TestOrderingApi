using Ordering.Application.Orders;

namespace Ordering.Application.Abstractions;

/// <summary>Acceso a lectura: devuelve DTOs, sin seguimiento de entidades.</summary>
public interface IOrderReadService
{
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
