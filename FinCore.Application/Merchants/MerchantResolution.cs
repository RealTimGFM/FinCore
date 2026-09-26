using FinCore.Domain.Merchants;

namespace FinCore.Application.Merchants;

public sealed record MerchantResolution(
    Guid MerchantId,
    string CanonicalName,
    string MatchedAlias,
    MerchantAliasMatchType MatchType);
