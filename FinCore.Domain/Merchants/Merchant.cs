namespace FinCore.Domain.Merchants;

public sealed class Merchant
{
    public Guid Id { get; private set; }

    public string CanonicalName { get; private set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Merchant()
    {
        // EF Core
    }

    public static Merchant Create(string canonicalName)
    {
        if (string.IsNullOrWhiteSpace(canonicalName))
        {
            throw new ArgumentException(
                "Merchant name is required.",
                nameof(canonicalName));
        }

        canonicalName = canonicalName.Trim();

        if (canonicalName.Length > 100)
        {
            throw new ArgumentException(
                "Merchant name cannot exceed 100 characters.",
                nameof(canonicalName));
        }

        return new Merchant
        {
            Id = Guid.NewGuid(),
            CanonicalName = canonicalName,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
