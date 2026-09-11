using FinCore.Application.Common.Persistence;
using FinCore.Domain.Categories;

namespace FinCore.Application.Categories;

public sealed class CategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CategoryDto> CreateAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var category = Category.Create(command.Name);

        await _categoryRepository.AddAsync(
            category,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(category);
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var categories =
            await _categoryRepository.GetAllAsync(
                includeArchived,
                cancellationToken);

        return categories
            .Select(Map)
            .ToList();
    }

    public async Task<CategoryDto?> RenameAsync(
        Guid id,
        string newName,
        CancellationToken cancellationToken = default)
    {
        var category =
            await _categoryRepository.GetByIdForUpdateAsync(
                id,
                cancellationToken);

        if (category is null)
        {
            return null;
        }

        category.Rename(newName);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(category);
    }

    public async Task<CategoryDto?> ArchiveAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category =
            await _categoryRepository.GetByIdForUpdateAsync(
                id,
                cancellationToken);

        if (category is null)
        {
            return null;
        }

        category.Archive();

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(category);
    }

    private static CategoryDto Map(Category category)
    {
        return new CategoryDto(
            category.Id,
            category.Name,
            category.IsArchived,
            category.CreatedAtUtc);
    }
}
