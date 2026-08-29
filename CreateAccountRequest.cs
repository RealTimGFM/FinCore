using FinCore.Domain.Accounts;

namespace FinCore.Api.Contracts.Accounts;

public sealed record CreateAccountRequest(
    string Name,
    AccountType Type,
    string Currency
    );