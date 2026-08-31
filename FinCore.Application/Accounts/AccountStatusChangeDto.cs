using FinCore.Domain.Accounts;

namespace FinCore.Application.Accounts;

public sealed record AccountStatusChangeDto(
    Guid Id,
    Guid AccountId,
    AccountStatus FromStatus,
    AccountStatus ToStatus,
    string Reason,
    AccountStatusChangeSource Source,
    DateTimeOffset ChangedAtUtc
);
