using CardBack.Application.Ports;
using CardBack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CardBack.Infrastructure.Persistence;

public sealed class CardRepository : ICardRepository
{
    private readonly AppDbContext _db;

    public CardRepository(AppDbContext db) => _db = db;

    public Task<List<Card>> ListByUserAsync(Guid userId, CancellationToken ct = default)
        => _db.Cards.Where(c => c.UserId == userId).ToListAsync(ct);

    public Task<Card?> FindByIdAsync(Guid cardId, CancellationToken ct = default)
        => _db.Cards.FirstOrDefaultAsync(c => c.Id == cardId, ct);

    public async Task AddAsync(Card card, CancellationToken ct = default)
    {
        _db.Cards.Add(card);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Card card, CancellationToken ct = default)
    {
        _db.Cards.Update(card);
        await _db.SaveChangesAsync(ct);
    }
}
