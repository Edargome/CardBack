namespace CardBack.Application.Cards;

public sealed record CreateCardRequest(string Brand, string Last4, string Token, string Nickname);

public sealed record CardDto(
    Guid Id,
    string Brand,
    string Last4,
    string Nickname,
    DateTimeOffset CreatedAt
);
