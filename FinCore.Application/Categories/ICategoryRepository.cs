using FinCore.Domain.Categories;

namespace FinCore.Application.Categories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Category?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> GetAllAsync(
        bool includeArchived,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Category category,
        CancellationToken cancellationToken = default);
}
