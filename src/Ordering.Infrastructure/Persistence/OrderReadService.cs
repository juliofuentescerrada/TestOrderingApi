using Microsoft.EntityFrameworkCore;
using Ordering.Application.Abstractions;
using Ordering.Application.Orders;

namespace Ordering.Infrastructure.Persistence;

public sealed class OrderReadService : IOrderReadService
{
    private readonly AppDbContext _db;

    public OrderReadService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);

        return order is null ? null : OrderDto.From(order);
    }
}
