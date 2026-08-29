namespace FinCore.Domain.Accounts;

public sealed class Account
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public AccountType Type { get; private set; }

    public string Currency { get; private set; } = null!;

    // Cached/materialized balance.
    // The ledger will eventually become the source of truth.
    public decimal Balance { get; private set; }

    public DateTimeOffset BalanceAsOfUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

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

        if (currency.Length != 3)
        {
            throw new ArgumentException(
                "Currency must use a 3-letter code such as CAD or USD.",
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
}