namespace FinCore.Domain.Accounts;

public sealed class Account
{
    private static readonly HashSet<string> SupportedCurrencies =
        new(StringComparer.Ordinal)
        {
            "CAD",
            "USD",
            "VND"
        };

    private readonly List<AccountStatusChange> _statusChanges = [];
    public byte[] RowVersion { get; private set; } = [];
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public AccountType Type { get; private set; }

    public string Currency { get; private set; } = null!;

    public AccountStatus Status { get; private set; }

    // Cached/materialized balance.
    // The ledger will eventually become the source of truth.
    public decimal Balance { get; private set; }

    public DateTimeOffset BalanceAsOfUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public IReadOnlyCollection<AccountStatusChange> StatusChanges =>
        _statusChanges.AsReadOnly();

    // Required by EF Core.
    private Account()
    {
    }

    private Account(
        Guid id,
        string name,
        AccountType type,
        string currency)
    {
        Id = id;
        Name = name;
        Type = type;
        Currency = currency;

        Status = AccountStatus.Active;
        ClosedAtUtc = null;

        Balance = 0m;

        CreatedAtUtc = DateTimeOffset.UtcNow;
        BalanceAsOfUtc = CreatedAtUtc;
    }

    public static Account Create(
        string name,
        AccountType type,
        string currency)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Account name is required.",
                nameof(name));
        }

        name = name.Trim();

        if (name.Length > 100)
        {
            throw new ArgumentException(
                "Account name cannot exceed 100 characters.",
                nameof(name));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentException(
                "Invalid account type.",
                nameof(type));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException(
                "Currency is required.",
                nameof(currency));
        }

        currency = currency.Trim().ToUpperInvariant();

        if (!SupportedCurrencies.Contains(currency))
        {
            throw new ArgumentException(
                "Currency must be one of the supported currencies: CAD, USD, or VND.",
                nameof(currency));
        }

        return new Account(
            Guid.NewGuid(),
            name,
            type,
            currency);
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException(
                "Account name is required.",
                nameof(newName));
        }

        newName = newName.Trim();

        if (newName.Length > 100)
        {
            throw new ArgumentException(
                "Account name cannot exceed 100 characters.",
                nameof(newName));
        }

        Name = newName;
    }

    public void RefreshBalanceSnapshot(
        decimal balance,
        DateTimeOffset asOfUtc)
    {
        Balance = balance;
        BalanceAsOfUtc = asOfUtc;
    }

    public void Close(
        string reason,
        AccountStatusChangeSource source)
    {
        if (Status != AccountStatus.Active)
        {
            throw new InvalidOperationException(
                "Only an active account can be closed.");
        }

        reason = ValidateReason(reason);
        ValidateSource(source);

        var changedAtUtc = DateTimeOffset.UtcNow;

        Status = AccountStatus.Closed;
        ClosedAtUtc = changedAtUtc;

        _statusChanges.Add(AccountStatusChange.Create(
            Id,
            AccountStatus.Active,
            AccountStatus.Closed,
            reason,
            source,
            changedAtUtc));
    }

    public void Reopen(
        string reason,
        AccountStatusChangeSource source)
    {
        if (Status != AccountStatus.Closed)
        {
            throw new InvalidOperationException(
                "Only a closed account can be reopened.");
        }

        reason = ValidateReason(reason);
        ValidateSource(source);

        var changedAtUtc = DateTimeOffset.UtcNow;

        Status = AccountStatus.Active;
        ClosedAtUtc = null;

        _statusChanges.Add(AccountStatusChange.Create(
            Id,
            AccountStatus.Closed,
            AccountStatus.Active,
            reason,
            source,
            changedAtUtc));
    }

    private static string ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "A reason is required.",
                nameof(reason));
        }

        reason = reason.Trim();

        if (reason.Length > 250)
        {
            throw new ArgumentException(
                "Reason cannot exceed 250 characters.",
                nameof(reason));
        }

        return reason;
    }

    private static void ValidateSource(
        AccountStatusChangeSource source)
    {
        if (!Enum.IsDefined(source))
        {
            throw new ArgumentException(
                "Invalid account status change source.",
                nameof(source));
        }
    }

    public void ApplyTransaction(
    decimal amount,
    DateTimeOffset appliedAtUtc)
    {
        if (Status != AccountStatus.Active)
        {
            throw new InvalidOperationException(
                "Transactions cannot be added to a closed account.");
        }

        if (amount == 0m)
        {
            throw new ArgumentException(
                "Transaction amount cannot be zero.",
                nameof(amount));
        }

        Balance += amount;
        BalanceAsOfUtc = appliedAtUtc;
    }
}
