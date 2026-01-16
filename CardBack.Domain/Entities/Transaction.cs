namespace CardBack.Domain.Entities;

public enum TransactionStatus
{
    Approved = 1,
    Declined = 2,
    Reversed = 3
}

public sealed class Transaction
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid CardId { get; private set; }
    public Guid UserId { get; private set; }  // redundante para consultas rápidas y seguridad

    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "COP";

    public string Description { get; private set; } = string.Empty;
    public TransactionStatus Status { get; private set; } = TransactionStatus.Approved;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Transaction() { }

    public Transaction(Guid userId, Guid cardId, decimal amount, string currency, string description, TransactionStatus status)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.");
        if (cardId == Guid.Empty) throw new ArgumentException("CardId is required.");
        if (amount <= 0) throw new ArgumentException("Amount must be > 0.");
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.");

        UserId = userId;
        CardId = cardId;
        Amount = decimal.Round(amount, 2);
        Currency = currency.Trim().ToUpperInvariant();
        Description = (description ?? string.Empty).Trim();
        Status = status;
    }

    public void Reverse()
    {
        if (Status != TransactionStatus.Approved)
            throw new InvalidOperationException("Only approved transactions can be reversed.");

        Status = TransactionStatus.Reversed;
    }
}
