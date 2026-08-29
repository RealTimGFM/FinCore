using FinCore.Domain.Accounts;

namespace FinCore.Application.Accounts;

public sealed record AccountDto(
    Guid Id,
    string Name,
    AccountType Type,
    string Currency,
    decimal Balance,
    DateTimeOffset BalanceAsOfUtc,
    DateTimeOffset CreatedAtUtc
);