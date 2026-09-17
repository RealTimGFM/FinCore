using FinCore.Application.Categories;
using FinCore.Application.Merchants;

namespace FinCore.Application.Transactions.Classification;

public sealed class TransactionCategoryClassifier
{
    private readonly IMerchantMemoryRepository _merchantMemoryRepository;
    private readonly ICategoryRepository _categoryRepository;

    public TransactionCategoryClassifier(
        IMerchantMemoryRepository merchantMemoryRepository,
        ICategoryRepository categoryRepository)
    {
        _merchantMemoryRepository = merchantMemoryRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<ClassificationResult> ClassifyAsync(
        string description,
        CancellationToken cancellationToken = default)
    {
        var normalizedMerchant = MerchantNormalizer.Normalize(description);

        var memory = await _merchantMemoryRepository
            .GetByNormalizedMerchantAsync(
                normalizedMerchant,
                cancellationToken);

        if (memory is null)
        {
            return ClassificationResult.NoMatch;
        }

        var category = await _categoryRepository.GetByIdAsync(
            memory.CategoryId,
            cancellationToken);

        if (category is null || category.IsArchived)
        {
            return ClassificationResult.NoMatch;
        }

        return ClassificationResult.FromMerchantMemory(category.Id);
    }
}
