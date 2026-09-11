namespace FinCore.Api.Contracts.Transactions;

public sealed record SetTransactionCategoryRequest(
    Guid? CategoryId);
