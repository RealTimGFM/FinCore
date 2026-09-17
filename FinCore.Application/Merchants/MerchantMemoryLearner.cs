using FinCore.Domain.Merchants;

namespace FinCore.Application.Merchants;

public sealed class MerchantMemoryLearner
{
    private readonly IMerchantMemoryRepository
        _merchantMemoryRepository;

    public MerchantMemoryLearner(
        IMerchantMemoryRepository merchantMemoryRepository)
    {
        _merchantMemoryRepository =
            merchantMemoryRepository;
    }

    public async Task<MerchantMemory> LearnAsync(
        string rawMerchant,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var normalizedMerchant =
            MerchantNormalizer.Normalize(rawMerchant);

        var memory =
            await _merchantMemoryRepository
                .GetByNormalizedMerchantForUpdateAsync(
                    normalizedMerchant,
                    cancellationToken);

        if (memory is null)
        {
            memory = MerchantMemory.Create(
                normalizedMerchant,
                categoryId);

            await _merchantMemoryRepository.AddAsync(
                memory,
                cancellationToken);

            return memory;
        }

        memory.RememberCategory(categoryId);

        return memory;
    }
}
