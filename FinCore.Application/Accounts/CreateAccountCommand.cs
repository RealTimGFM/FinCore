using FinCore.Domain.Accounts;

namespace FinCore.Application.Accounts;

public sealed record CreateAccountCommand(
    string Name,
    AccountType Type,
    string Currency
    );