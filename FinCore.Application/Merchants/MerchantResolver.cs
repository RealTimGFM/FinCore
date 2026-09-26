using FinCore.Domain.Merchants;

namespace FinCore.Application.Merchants;

public sealed class MerchantResolver
{
    private readonly ISharedMerchantCatalogRepository _catalog;

    public MerchantResolver(ISharedMerchantCatalogRepository catalog)
    {
        _catalog = catalog;
    }

    public async Task<MerchantResolution?> ResolveAsync(
        string rawDescription,
        CancellationToken cancellationToken = default)
    {
        var normalized = MerchantNormalizer.Normalize(rawDescription);
        var prefixes = BuildPrefixes(normalized);

        var candidates = await _catalog.FindCandidatesAsync(
            prefixes,
            cancellationToken);

        // Exact matches have priority over pattern matches.
        var exactMatches = candidates
            .Where(candidate =>
                candidate.MatchType == MerchantAliasMatchType.Exact &&
                candidate.NormalizedAlias == normalized)
            .ToList();

        if (exactMatches.Count > 0)
        {
            return ToUniqueResolution(exactMatches);
        }

        var patternMatches = candidates
            .Where(candidate =>
                candidate.MatchType == MerchantAliasMatchType.NumericStorePrefix &&
                HasNumericStoreSuffix(normalized, candidate.NormalizedAlias))
            .ToList();

        if (patternMatches.Count == 0)
        {
            return null;
        }

        // A more specific matching alias wins.
        var longestLength = patternMatches.Max(
            candidate => candidate.NormalizedAlias.Length);

        var mostSpecific = patternMatches
            .Where(candidate =>
                candidate.NormalizedAlias.Length == longestLength)
            .ToList();

        return ToUniqueResolution(mostSpecific);
    }

    private static IReadOnlyCollection<string> BuildPrefixes(string normalized)
    {
        var prefixes = new List<string> { normalized };
        var position = normalized.LastIndexOf(' ');

        while (position > 0)
        {
            prefixes.Add(normalized[..position]);
            position = normalized.LastIndexOf(' ', position - 1);
        }

        return prefixes;
    }

    private static bool HasNumericStoreSuffix(string description, string alias)
    {
        if (!description.StartsWith(alias + " ", StringComparison.Ordinal))
        {
            return false;
        }

        var suffix = description[(alias.Length + 1)..];
        var firstSpace = suffix.IndexOf(' ');
        var storeNumber = firstSpace < 0 ? suffix : suffix[..firstSpace];

        return storeNumber.Length > 0 && storeNumber.All(char.IsDigit);
    }

    private static MerchantResolution? ToUniqueResolution(
        IReadOnlyList<MerchantAliasCandidate> matches)
    {
        if (matches.Count == 0)
        {
            return null;
        }

        var merchantIds = matches
            .Select(match => match.MerchantId)
            .Distinct()
            .ToList();

        // Ambiguous identity: don't guess.
        if (merchantIds.Count != 1)
        {
            return null;
        }

        var match = matches[0];

        return new MerchantResolution(
            match.MerchantId,
            match.CanonicalName,
            match.NormalizedAlias,
            match.MatchType);
    }
}
