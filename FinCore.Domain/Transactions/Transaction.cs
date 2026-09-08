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

    /// <summary>
    /// If this transaction reverses another transaction,
    /// this contains the original transaction's ID.
    /// Otherwise null.
    /// </summary>
    public Guid? ReversalOfTransactionId { get; private set; }

    public bool IsReversal =>
        ReversalOfTransactionId.HasValue;

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
        DateTimeOffset createdAtUtc,
        Guid? reversalOfTransactionId)
    {
        Id = id;
        AccountId = accountId;
        Amount = amount;
        Description = description;
        OccurredAtUtc = occurredAtUtc;
        CreatedAtUtc = createdAtUtc;
        ReversalOfTransactionId = reversalOfTransactionId;
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

        description = ValidateDescription(
            description);

        return new Transaction(
            Guid.NewGuid(),
            accountId,
            amount,
            description,
            occurredAtUtc,
            DateTimeOffset.UtcNow,
            null);
    }

    public static Transaction CreateReversal(
        Transaction originalTransaction,
        string description,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(
            originalTransaction);

        if (originalTransaction.IsReversal)
        {
            throw new InvalidOperationException(
                "A reversal transaction cannot itself be reversed.");
        }

        description = ValidateDescription(
            description);

        return new Transaction(
            Guid.NewGuid(),
            originalTransaction.AccountId,
            -originalTransaction.Amount,
            description,
            occurredAtUtc,
            DateTimeOffset.UtcNow,
            originalTransaction.Id);
    }

    private static string ValidateDescription(
        string description)
    {
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

        return description;
    }
}
