using CardBack.Application.Ports;
using CardBack.Domain.Entities;

namespace CardBack.Application.Transactions;

public sealed class TransactionService
{
    private readonly IUserRepository _users;
    private readonly ICardRepository _cards;
    private readonly ITransactionRepository _txRepo;

    public TransactionService(IUserRepository users, ICardRepository cards, ITransactionRepository txRepo)
    {
        _users = users;
        _cards = cards;
        _txRepo = txRepo;
    }

    public async Task<TransactionDto> PayAsync(Guid userId, CreateTransactionRequest req, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId, ct);
        if (user is null || !user.IsActive) throw new UnauthorizedAccessException("Invalid user.");

        var card = await _cards.FindByIdAsync(req.CardId, ct);
        if (card is null || !card.IsActive) throw new InvalidOperationException("Card not found.");

        // Seguridad: la tarjeta debe pertenecer al usuario logueado
        if (card.UserId != userId) throw new UnauthorizedAccessException("Not allowed.");

        // Simulación de aprobación/rechazo (puedes cambiar lógica después)
        var status = req.Amount <= 2_000_000m ? TransactionStatus.Approved : TransactionStatus.Declined;

        var tx = new Transaction(
            userId: userId,
            cardId: card.Id,
            amount: req.Amount,
            currency: string.IsNullOrWhiteSpace(req.Currency) ? "COP" : req.Currency,
            description: req.Description,
            status: status
        );

        await _txRepo.AddAsync(tx, ct);

        return new TransactionDto(tx.Id, tx.CardId, tx.Amount, tx.Currency, tx.Description, tx.Status, tx.CreatedAt);
    }

    public async Task<List<TransactionDto>> HistoryAsync(Guid userId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId, ct);
        if (user is null || !user.IsActive) throw new UnauthorizedAccessException("Invalid user.");

        var list = await _txRepo.ListByUserAsync(userId, from, to, ct);

        return list
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new TransactionDto(x.Id, x.CardId, x.Amount, x.Currency, x.Description, x.Status, x.CreatedAt))
            .ToList();
    }

    public async Task<List<TransactionDto>> HistoryByCardAsync(Guid userId, Guid cardId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId, ct);
        if (user is null || !user.IsActive) throw new UnauthorizedAccessException("Invalid user.");

        var list = await _txRepo.ListByCardAsync(userId, cardId, ct);

        return list
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new TransactionDto(x.Id, x.CardId, x.Amount, x.Currency, x.Description, x.Status, x.CreatedAt))
            .ToList();
    }
}
