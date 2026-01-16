namespace CardBack.Domain.Entities;

public sealed class Card
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid UserId { get; private set; }

    public string Brand { get; private set; } = string.Empty;     // Visa, MasterCard, etc.
    public string Last4 { get; private set; } = string.Empty;     // "1234"
    public string Token { get; private set; } = string.Empty;     // token/alias (NO guardar PAN real)
    public string Nickname { get; private set; } = string.Empty;  
    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Card() { }

    public Card(Guid userId, string brand, string last4, string token, string nickname = "")
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.");
        if (string.IsNullOrWhiteSpace(brand)) throw new ArgumentException("Brand is required.");
        if (string.IsNullOrWhiteSpace(last4) || last4.Trim().Length != 4) throw new ArgumentException("Last4 must be 4 digits.");
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token is required.");

        UserId = userId;
        Brand = brand.Trim();
        Last4 = last4.Trim();
        Token = token.Trim();
        Nickname = (nickname ?? string.Empty).Trim();
    }

    public void Disable() => IsActive = false;
}
