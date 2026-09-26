using FinCore.Application.Merchants;
using FinCore.Domain.Merchants;

namespace FinCore.Tests.Merchants;

public sealed class MerchantResolverTests
{
    [Fact]
    public async Task Resolve_NormalizesAndQueriesOnlyWordPrefixes()
    {
        var catalog = new StubCatalog([]);
        using var cancellation = new CancellationTokenSource();

        var result = await new MerchantResolver(catalog).ResolveAsync(
            "  Starbucks #04215 Montreal QC  ", cancellation.Token);

        Assert.Null(result);
        Assert.Equal(
            ["STARBUCKS 04215 MONTREAL QC", "STARBUCKS 04215 MONTREAL",
             "STARBUCKS 04215", "STARBUCKS"],
            catalog.RequestedPrefixes);
        Assert.Equal(cancellation.Token, catalog.CancellationToken);
    }

    [Theory]
    [InlineData("STARBUCKS #1782")]
    [InlineData("starbucks 04215 Montreal QC")]
    public async Task Resolve_NumericStorePrefixMatches(string description)
    {
        var candidate = Candidate("STARBUCKS", MerchantAliasMatchType.NumericStorePrefix);
        var resolver = new MerchantResolver(new StubCatalog([candidate]));

        var result = await resolver.ResolveAsync(description);

        Assert.Equal(new MerchantResolution(
            candidate.MerchantId, candidate.CanonicalName,
            candidate.NormalizedAlias, candidate.MatchType), result);
    }

    [Theory]
    [InlineData("STARBUCKS")]
    [InlineData("STARBUCKSIFIED 1782")]
    [InlineData("THE STARBUCKS STORE")]
    [InlineData("STARBUCKS MOBILE")]
    [InlineData("STARBUCKS 1782A")]
    [InlineData("STARBUCKS A1782")]
    [InlineData("STARBUCKS1782")]
    [InlineData("###")]
    public async Task Resolve_NumericStorePrefixRejectsUnsupportedDescriptions(string description)
    {
        var resolver = new MerchantResolver(new StubCatalog(
            [Candidate("STARBUCKS", MerchantAliasMatchType.NumericStorePrefix)]));

        Assert.Null(await resolver.ResolveAsync(description));
    }

    [Theory]
    [InlineData("Starbucks Mobile", "STARBUCKS MOBILE")]
    [InlineData("STARBUCKS.COM", "STARBUCKS COM")]
    public async Task Resolve_ExactMatchesOnlyFullNormalizedDescription(
        string description, string alias)
    {
        var candidate = Candidate(alias, MerchantAliasMatchType.Exact);
        var resolver = new MerchantResolver(new StubCatalog([candidate]));

        Assert.Equal(candidate.MerchantId, (await resolver.ResolveAsync(description))?.MerchantId);
        Assert.Null(await resolver.ResolveAsync(description + " EXTRA"));
    }

    [Fact]
    public async Task Resolve_ExactMatchTakesPriorityOverPattern()
    {
        var exact = Candidate("STARBUCKS 1782", MerchantAliasMatchType.Exact);
        var pattern = Candidate("STARBUCKS", MerchantAliasMatchType.NumericStorePrefix);
        var resolver = new MerchantResolver(new StubCatalog([pattern, exact]));

        Assert.Equal(exact.MerchantId, (await resolver.ResolveAsync("STARBUCKS #1782"))?.MerchantId);
    }

    [Fact]
    public async Task Resolve_LongestMatchingPatternWins()
    {
        var broad = Candidate("STORE", MerchantAliasMatchType.NumericStorePrefix);
        var specific = Candidate("STORE 24", MerchantAliasMatchType.NumericStorePrefix);
        var resolver = new MerchantResolver(new StubCatalog([broad, specific]));

        Assert.Equal(specific.MerchantId, (await resolver.ResolveAsync("STORE 24 123 CITY"))?.MerchantId);
    }

    [Theory]
    [InlineData(MerchantAliasMatchType.Exact, "STORE 123")]
    [InlineData(MerchantAliasMatchType.NumericStorePrefix, "STORE")]
    public async Task Resolve_ConflictingIdentitiesReturnNoMatch(
        MerchantAliasMatchType matchType, string alias)
    {
        var resolver = new MerchantResolver(new StubCatalog(
        [
            Candidate(alias, matchType),
            Candidate(alias, matchType),
            Candidate("STORE", MerchantAliasMatchType.NumericStorePrefix)
        ]));

        Assert.Null(await resolver.ResolveAsync("STORE 123"));
    }

    [Fact]
    public async Task Resolve_DuplicateCandidatesForSameIdentityAreNotAmbiguous()
    {
        var candidate = Candidate("STORE", MerchantAliasMatchType.Exact);
        var resolver = new MerchantResolver(new StubCatalog([candidate, candidate]));

        Assert.Equal(candidate.MerchantId, (await resolver.ResolveAsync("STORE"))?.MerchantId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Resolve_MissingDescriptionIsRejectedBeforeQuery(string? description)
    {
        var catalog = new StubCatalog([]);

        await Assert.ThrowsAsync<ArgumentException>(
            () => new MerchantResolver(catalog).ResolveAsync(description!));
        Assert.Null(catalog.RequestedPrefixes);
    }

    private static MerchantAliasCandidate Candidate(string alias, MerchantAliasMatchType matchType)
        => new(Guid.NewGuid(), "Example merchant", alias, matchType);

    private sealed class StubCatalog(IReadOnlyList<MerchantAliasCandidate> candidates)
        : ISharedMerchantCatalogRepository
    {
        public IReadOnlyCollection<string>? RequestedPrefixes { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<IReadOnlyList<MerchantAliasCandidate>> FindCandidatesAsync(
            IReadOnlyCollection<string> normalizedPrefixes,
            CancellationToken cancellationToken = default)
        {
            RequestedPrefixes = normalizedPrefixes;
            CancellationToken = cancellationToken;
            return Task.FromResult(candidates);
        }
    }
}
