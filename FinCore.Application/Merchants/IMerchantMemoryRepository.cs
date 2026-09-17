using FinCore.Domain.Merchants;

namespace FinCore.Application.Merchants;

public interface IMerchantMemoryRepository
{
    Task<MerchantMemory?> GetByNormalizedMerchantAsync(
        string normalizedMerchant,
        CancellationToken cancellationToken = default);

    Task<MerchantMemory?> GetByNormalizedMerchantForUpdateAsync(
        string normalizedMerchant,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MerchantMemory memory,
        CancellationToken cancellationToken = default);
}
