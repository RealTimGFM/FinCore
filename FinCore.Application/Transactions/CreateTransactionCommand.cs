namespace FinCore.Application.Transactions;

public sealed record CreateTransactionCommand(
    Guid AccountId,
    decimal Amount,
    string Description,
    DateTimeOffset OccurredAtUtc);
