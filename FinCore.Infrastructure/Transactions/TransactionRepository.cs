using FinCore.Application.Transactions;
using FinCore.Domain.Transactions;
using FinCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Infrastructure.Transactions;

public sealed class TransactionRepository
    : ITransactionRepository
{
    private readonly FinCoreDbContext _dbContext;

    public TransactionRepository(
        FinCoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Transaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                transaction => transaction.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetByAccountIdAsync(
        Guid accountId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.AccountId == accountId)
            .OrderByDescending(transaction =>
                transaction.OccurredAtUtc)
            .ThenByDescending(transaction =>
                transaction.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions
            .AsNoTracking()
            .CountAsync(
                transaction =>
                    transaction.AccountId == accountId,
                cancellationToken);
    }

    public async Task<bool> HasReversalAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions
            .AsNoTracking()
            .AnyAsync(
                transaction =>
                    transaction.ReversalOfTransactionId ==
                    transactionId,
                cancellationToken);
    }

    public async Task AddAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Transactions.AddAsync(
            transaction,
            cancellationToken);
    }
}
