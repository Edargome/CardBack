using Microsoft.EntityFrameworkCore;
using CardBack.Application.Ports;
using CardBack.Domain.Entities;

namespace CardBack.Infrastructure.Persistence;

public sealed class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _db;

    public TransactionRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Transaction tx, CancellationToken ct = default)
    {
        _db.Transactions.Add(tx);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<Transaction>> ListByUserAsync(Guid userId, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken ct = default)
    {
        var q = _db.Transactions.Where(x => x.UserId == userId);

        if (from.HasValue) q = q.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(x => x.CreatedAt <= to.Value);

        return q.ToListAsync(ct);
    }

    public Task<List<Transaction>> ListByCardAsync(Guid userId, Guid cardId, CancellationToken ct = default)
    {
        // userId para evitar filtrar por una card ajena
        return _db.Transactions
            .Where(x => x.UserId == userId && x.CardId == cardId)
            .ToListAsync(ct);
    }
}
