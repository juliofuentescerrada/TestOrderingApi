using Microsoft.Data.Sqlite;
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

    public Task<IdempotencyRecord?> FindIdempotencyRecordAsync(string subject, string key, CancellationToken cancellationToken) =>
        _db.IdempotencyRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Subject == subject && r.Key == key, cancellationToken);

    public async Task AddIdempotencyRecordAsync(IdempotencyRecord record, CancellationToken cancellationToken) =>
        await _db.IdempotencyRecords.AddAsync(record, cancellationToken);

    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        // Se captura aquí y no en Application para que EF/SQLite no se filtren a la capa de aplicación (ADR-0011).
        // 1555 = SQLITE_CONSTRAINT_PRIMARYKEY: otra petición guardó antes la misma (Subject, Key).
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: 1555 })
        {
            // Sin esto el pedido que no se guardó seguiría rastreado y un SaveChanges posterior lo insertaría.
            _db.ChangeTracker.Clear();
            return false;
        }
    }
}
