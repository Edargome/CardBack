using CardBack.Application.Ports;
using CardBack.Domain.Entities;

namespace CardBack.Application.Cards;

public sealed class CardService
{
    private readonly IUserRepository _users;
    private readonly ICardRepository _cards;

    public CardService(IUserRepository users, ICardRepository cards)
    {
        _users = users;
        _cards = cards;
    }

    public async Task<List<CardDto>> ListMineAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId, ct);
        if (user is null || !user.IsActive) throw new UnauthorizedAccessException("Invalid user.");

        var cards = await _cards.ListByUserAsync(userId, ct);

        return cards
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CardDto(c.Id, c.Brand, c.Last4, c.Nickname, c.CreatedAt))
            .ToList();
    }

    public async Task<CardDto> CreateAsync(Guid userId, CreateCardRequest req, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId, ct);
        if (user is null || !user.IsActive) throw new UnauthorizedAccessException("Invalid user.");

        var card = new Card(userId, req.Brand, req.Last4, req.Token, req.Nickname ?? "");
        await _cards.AddAsync(card, ct);

        return new CardDto(card.Id, card.Brand, card.Last4, card.Nickname, card.CreatedAt);
    }

    public async Task DeleteAsync(Guid userId, Guid cardId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId, ct);
        if (user is null || !user.IsActive) throw new UnauthorizedAccessException("Invalid user.");

        var card = await _cards.FindByIdAsync(cardId, ct);
        if (card is null || !card.IsActive) return; // idempotente

        // Seguridad: solo el dueño puede eliminar
        if (card.UserId != userId) throw new UnauthorizedAccessException("Not allowed.");

        // Borrado lógico recomendado
        card.Disable();
        await _cards.UpdateAsync(card, ct);
    }
}
