namespace FinCore.Api.Contracts.Transactions;

public sealed record ReverseTransactionRequest(
    string Description,
    DateTimeOffset OccurredAtUtc);
