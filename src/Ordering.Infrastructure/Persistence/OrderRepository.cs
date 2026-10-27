using Microsoft.EntityFrameworkCore;
using Ordering.Application.Abstractions;
using Ordering.Domain;

namespace Ordering.Infrastructure.Persistence;

public sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;

    public OrderRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Orders
            .Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task AddAsync(Order order, CancellationToken cancellationToken) =>
        await _db.Orders.AddAsync(order, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);
}
