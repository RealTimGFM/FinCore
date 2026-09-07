namespace FinCore.Domain.Transactions;

public sealed class Transaction
{
    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    /// <summary>
    /// Signed effect on the account balance.
    /// Positive increases the balance.
    /// Negative decreases the balance.
    /// </summary>
    public decimal Amount { get; private set; }

    public string Description { get; private set; } = null!;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    // Required by EF Core.
    private Transaction()
    {
    }

    private Transaction(
        Guid id,
        Guid accountId,
        decimal amount,
        string description,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        AccountId = accountId;
        Amount = amount;
        Description = description;
        OccurredAtUtc = occurredAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public static Transaction Create(
        Guid accountId,
        decimal amount,
        string description,
        DateTimeOffset occurredAtUtc)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException(
                "Account ID is required.",
                nameof(accountId));
        }

        if (amount == 0m)
        {
            throw new ArgumentException(
                "Transaction amount cannot be zero.",
                nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "Transaction description is required.",
                nameof(description));
        }

        description = description.Trim();

        if (description.Length > 200)
        {
            throw new ArgumentException(
                "Transaction description cannot exceed 200 characters.",
                nameof(description));
        }

        return new Transaction(
            Guid.NewGuid(),
            accountId,
            amount,
            description,
            occurredAtUtc,
            DateTimeOffset.UtcNow);
    }
}