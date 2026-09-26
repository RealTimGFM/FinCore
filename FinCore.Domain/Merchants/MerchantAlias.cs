namespace FinCore.Domain.Merchants;

public sealed class MerchantAlias
{
    public Guid Id { get; private set; }

    public Guid MerchantId { get; private set; }

    public string NormalizedAlias { get; private set; } = null!;

    public MerchantAliasMatchType MatchType { get; private set; }

    private MerchantAlias()
    {
        // EF Core
    }

    public static MerchantAlias Create(
        Guid merchantId,
        string normalizedAlias,
        MerchantAliasMatchType matchType)
    {
        if (merchantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Merchant ID is required.",
                nameof(merchantId));
        }

        if (string.IsNullOrWhiteSpace(normalizedAlias))
        {
            throw new ArgumentException(
                "Normalized alias is required.",
                nameof(normalizedAlias));
        }

        normalizedAlias = normalizedAlias.Trim();

        if (normalizedAlias.Length > 200)
        {
            throw new ArgumentException(
                "Normalized alias cannot exceed 200 characters.",
                nameof(normalizedAlias));
        }

        if (!Enum.IsDefined(matchType))
        {
            throw new ArgumentException(
                "Invalid merchant alias match type.",
                nameof(matchType));
        }

        return new MerchantAlias
        {
            Id = Guid.NewGuid(),
            MerchantId = merchantId,
            NormalizedAlias = normalizedAlias,
            MatchType = matchType
        };
    }
}
