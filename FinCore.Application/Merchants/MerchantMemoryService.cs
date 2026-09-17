using FinCore.Application.Categories;
using FinCore.Application.Common.Persistence;
using FinCore.Domain.Merchants;

namespace FinCore.Application.Merchants;

public sealed class MerchantMemoryService
{
    private readonly IMerchantMemoryRepository _merchantMemoryRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly MerchantMemoryLearner  _merchantMemoryLearner;

    public MerchantMemoryService(
        IMerchantMemoryRepository merchantMemoryRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        MerchantMemoryLearner  merchantMemoryLearner)
    {
        _merchantMemoryRepository = merchantMemoryRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _merchantMemoryLearner = merchantMemoryLearner;
    }

    public async Task<MerchantMemoryDto> RememberAsync(
        string rawMerchant,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(
            categoryId,
            cancellationToken);

        if (category is null)
        {
            throw new KeyNotFoundException("Category was not found.");
        }

        if (category.IsArchived)
        {
            throw new InvalidOperationException(
                "An archived category cannot be remembered for a merchant.");
        }

        var memory = await _merchantMemoryLearner.LearnAsync(
            rawMerchant,
            categoryId,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(memory);
    }

    public async Task<MerchantMemoryDto?> FindAsync(
        string rawMerchant,
        CancellationToken cancellationToken = default)
    {
        var normalizedMerchant = MerchantNormalizer.Normalize(rawMerchant);

        var memory = await _merchantMemoryRepository
            .GetByNormalizedMerchantAsync(
                normalizedMerchant,
                cancellationToken);

        return memory is null ? null : Map(memory);
    }

    private static MerchantMemoryDto Map(MerchantMemory memory)
    {
        return new MerchantMemoryDto(
            memory.Id,
            memory.NormalizedMerchant,
            memory.CategoryId,
            memory.CreatedAtUtc,
            memory.UpdatedAtUtc);
    }
}
