namespace FinCore.Domain.Merchants;

public sealed class MerchantMemory
{
    public Guid Id { get; private set; }

    public string NormalizedMerchant { get; private set; } = null!;

    public Guid CategoryId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    // Required by EF Core.
    private MerchantMemory()
    {
    }

    private MerchantMemory(
        Guid id,
        string normalizedMerchant,
        Guid categoryId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        NormalizedMerchant = normalizedMerchant;
        CategoryId = categoryId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public static MerchantMemory Create(
        string normalizedMerchant,
        Guid categoryId)
    {
        normalizedMerchant = ValidateNormalizedMerchant(normalizedMerchant);
        ValidateCategoryId(categoryId);

        var now = DateTimeOffset.UtcNow;

        return new MerchantMemory(
            Guid.NewGuid(),
            normalizedMerchant,
            categoryId,
            now);
    }

    public void RememberCategory(Guid categoryId)
    {
        ValidateCategoryId(categoryId);

        CategoryId = categoryId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string ValidateNormalizedMerchant(string normalizedMerchant)
    {
        if (string.IsNullOrWhiteSpace(normalizedMerchant))
        {
            throw new ArgumentException(
                "Normalized merchant is required.",
                nameof(normalizedMerchant));
        }

        normalizedMerchant = normalizedMerchant.Trim();

        if (normalizedMerchant.Length > 200)
        {
            throw new ArgumentException(
                "Normalized merchant cannot exceed 200 characters.",
                nameof(normalizedMerchant));
        }

        return normalizedMerchant;
    }

    private static void ValidateCategoryId(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Category ID is required.",
                nameof(categoryId));
        }
    }
}
