using FinCore.Application.Merchants;
using FinCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Infrastructure.Merchants;

public sealed class SharedMerchantCatalogRepository
    : ISharedMerchantCatalogRepository
{
    private readonly FinCoreDbContext _dbContext;

    public SharedMerchantCatalogRepository(FinCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MerchantAliasCandidate>> FindCandidatesAsync(
        IReadOnlyCollection<string> normalizedPrefixes,
        CancellationToken cancellationToken = default)
    {
        if (normalizedPrefixes.Count == 0)
        {
            return [];
        }

        return await (
            from alias in _dbContext.MerchantAliases.AsNoTracking()
            join merchant in _dbContext.Merchants.AsNoTracking()
                on alias.MerchantId equals merchant.Id
            where normalizedPrefixes.Contains(alias.NormalizedAlias)
            select new MerchantAliasCandidate(
                merchant.Id,
                merchant.CanonicalName,
                alias.NormalizedAlias,
                alias.MatchType)
        ).ToListAsync(cancellationToken);
    }
}
