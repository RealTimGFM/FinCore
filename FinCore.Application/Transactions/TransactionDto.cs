namespace FinCore.Application.Transactions;

public sealed record TransactionDto(
    Guid Id,
    Guid AccountId,
    decimal Amount,
    string Description,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset CreatedAtUtc,
    Guid? ReversalOfTransactionId,
    bool IsReversal);
