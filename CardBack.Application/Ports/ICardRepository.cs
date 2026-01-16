using CardBack.Domain.Entities;

namespace CardBack.Application.Ports;

public interface ICardRepository
{
    Task<List<Card>> ListByUserAsync(Guid userId, CancellationToken ct = default);
    Task<Card?> FindByIdAsync(Guid cardId, CancellationToken ct = default);
    Task AddAsync(Card card, CancellationToken ct = default);
    Task UpdateAsync(Card card, CancellationToken ct = default);
}
