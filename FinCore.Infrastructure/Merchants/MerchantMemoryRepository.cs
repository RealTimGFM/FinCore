using FinCore.Application.Merchants;
using FinCore.Domain.Merchants;
using FinCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Infrastructure.Merchants;

public sealed class MerchantMemoryRepository : IMerchantMemoryRepository
{
    private readonly FinCoreDbContext _dbContext;

    public MerchantMemoryRepository(FinCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MerchantMemory?> GetByNormalizedMerchantAsync(
        string normalizedMerchant,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.MerchantMemories
            .AsNoTracking()
            .SingleOrDefaultAsync(
                memory => memory.NormalizedMerchant == normalizedMerchant,
                cancellationToken);
    }

    public async Task<MerchantMemory?> GetByNormalizedMerchantForUpdateAsync(
        string normalizedMerchant,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.MerchantMemories
            .SingleOrDefaultAsync(
                memory => memory.NormalizedMerchant == normalizedMerchant,
                cancellationToken);
    }

    public async Task AddAsync(
        MerchantMemory memory,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.MerchantMemories.AddAsync(memory, cancellationToken);
    }
}
