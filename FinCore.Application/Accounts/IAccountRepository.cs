using FinCore.Domain.Accounts;

namespace FinCore.Application.Accounts;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Account?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Account>> GetAllAsync(
        bool includeClosed,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountStatusChange>> GetStatusHistoryAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Account account,
        CancellationToken cancellationToken = default);
}
