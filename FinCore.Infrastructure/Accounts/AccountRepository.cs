using FinCore.Application.Accounts;
using FinCore.Domain.Accounts;
using FinCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Infrastructure.Accounts;

public sealed class AccountRepository
    : IAccountRepository
{
    private readonly FinCoreDbContext _dbContext;

    public AccountRepository(
        FinCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Account?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                account => account.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Accounts
            .AsNoTracking()
            .OrderBy(account => account.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Account account,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Accounts.AddAsync(
            account,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}