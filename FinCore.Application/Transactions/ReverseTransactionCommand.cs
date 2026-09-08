namespace FinCore.Application.Transactions;

public sealed record ReverseTransactionCommand(
    Guid TransactionId,
    string Description,
    DateTimeOffset OccurredAtUtc);
