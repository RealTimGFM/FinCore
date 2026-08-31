namespace FinCore.Domain.Accounts;

public sealed class AccountStatusChange
{
    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public AccountStatus FromStatus { get; private set; }

    public AccountStatus ToStatus { get; private set; }

    public string Reason { get; private set; } = null!;

    public AccountStatusChangeSource Source { get; private set; }

    public DateTimeOffset ChangedAtUtc { get; private set; }

    // Required by EF Core.
    private AccountStatusChange()
    {
    }

    private AccountStatusChange(
        Guid id,
        Guid accountId,
        AccountStatus fromStatus,
        AccountStatus toStatus,
        string reason,
        AccountStatusChangeSource source,
        DateTimeOffset changedAtUtc)
    {
        Id = id;
        AccountId = accountId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Reason = reason;
        Source = source;
        ChangedAtUtc = changedAtUtc;
    }

    internal static AccountStatusChange Create(
        Guid accountId,
        AccountStatus fromStatus,
        AccountStatus toStatus,
        string reason,
        AccountStatusChangeSource source,
        DateTimeOffset changedAtUtc)
    {
        return new AccountStatusChange(
            Guid.NewGuid(),
            accountId,
            fromStatus,
            toStatus,
            reason,
            source,
            changedAtUtc);
    }
}
