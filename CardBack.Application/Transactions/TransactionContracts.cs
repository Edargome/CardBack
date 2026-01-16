using CardBack.Domain.Entities;

namespace CardBack.Application.Transactions;

public sealed record CreateTransactionRequest(
    Guid CardId,
    decimal Amount,
    string Currency,
    string Description
);

public sealed record TransactionDto(
    Guid Id,
    Guid CardId,
    decimal Amount,
    string Currency,
    string Description,
    TransactionStatus Status,
    DateTimeOffset CreatedAt
);
