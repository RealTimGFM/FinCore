using FinCore.Domain.Merchants;

namespace FinCore.Application.Merchants;

public sealed record MerchantAliasCandidate(
    Guid MerchantId,
    string CanonicalName,
    string NormalizedAlias,
    MerchantAliasMatchType MatchType);
