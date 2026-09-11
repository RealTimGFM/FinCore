using FinCore.Application.Categories;
using FinCore.Domain.Categories;
using FinCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Infrastructure.Categories;

public sealed class CategoryRepository
    : ICategoryRepository
{
    private readonly FinCoreDbContext _dbContext;

    public CategoryRepository(
        FinCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Category?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                category => category.Id == id,
                cancellationToken);
    }

    public async Task<Category?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .FirstOrDefaultAsync(
                category => category.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Categories.AsNoTracking();

        if (!includeArchived)
        {
            query = query.Where(
                category => !category.IsArchived);
        }

        return await query
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Category category,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Categories.AddAsync(
            category,
            cancellationToken);
    }
}
