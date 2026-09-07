namespace FinCore.Api.Contracts.Transactions;

public sealed record CreateTransactionRequest(
    Guid AccountId,
    decimal Amount,
    string Description,
    DateTimeOffset OccurredAtUtc);
