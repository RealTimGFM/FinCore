namespace FinCore.Application.Merchants;

public interface ISharedMerchantCatalogRepository
{
    Task<IReadOnlyList<MerchantAliasCandidate>> FindCandidatesAsync(
        IReadOnlyCollection<string> normalizedPrefixes,
        CancellationToken cancellationToken = default);
}
