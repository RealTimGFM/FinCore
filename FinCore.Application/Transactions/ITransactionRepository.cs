using FinCore.Domain.Transactions;

namespace FinCore.Application.Transactions;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transaction>> GetByAccountIdAsync(
        Guid accountId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default);
}
