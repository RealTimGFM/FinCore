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
        bool includeClosed,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Accounts.AsNoTracking();

        if (!includeClosed)
        {
            query = query.Where(
                account => account.Status == AccountStatus.Active);
        }

        return await query
            .OrderBy(account => account.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Account?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Accounts
            .FirstOrDefaultAsync(
                account => account.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<AccountStatusChange>> GetStatusHistoryAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AccountStatusChanges
            .AsNoTracking()
            .Where(statusChange => statusChange.AccountId == accountId)
            .OrderBy(statusChange => statusChange.ChangedAtUtc)
            .ThenBy(statusChange => statusChange.Id)
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
