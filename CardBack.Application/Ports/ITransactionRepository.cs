using CardBack.Domain.Entities;

namespace CardBack.Application.Ports;

public interface ITransactionRepository
{
    Task AddAsync(Transaction tx, CancellationToken ct = default);
    Task<List<Transaction>> ListByUserAsync(Guid userId, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken ct = default);
    Task<List<Transaction>> ListByCardAsync(Guid userId, Guid cardId, CancellationToken ct = default);
}
