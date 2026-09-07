using FinCore.Application.Common;
using FinCore.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Infrastructure.Persistence;

public sealed class EfUnitOfWork
    : IUnitOfWork
{
    private readonly FinCoreDbContext _dbContext;

    public EfUnitOfWork(
        FinCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(
                "The data was changed by another request. Reload it and try again.",
                exception);
        }
    }
}
