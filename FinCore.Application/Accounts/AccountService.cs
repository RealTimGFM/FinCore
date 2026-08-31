using FinCore.Domain.Accounts;

namespace FinCore.Application.Accounts;

public sealed class AccountService
{
    private readonly IAccountRepository _accountRepository;

    public AccountService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<AccountDto> CreateAsync(
        CreateAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var account = Account.Create(
            command.Name,
            command.Type,
            command.Currency);

        await _accountRepository.AddAsync(
            account,
            cancellationToken);

        await _accountRepository.SaveChangesAsync(
            cancellationToken);

        return Map(account);
    }

    public async Task<AccountDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(
            id,
            cancellationToken);

        return account is null
            ? null
            : Map(account);
    }

    public async Task<IReadOnlyList<AccountDto>> GetAllAsync(
        bool includeClosed,
        CancellationToken cancellationToken = default)
    {
        var accounts = await _accountRepository.GetAllAsync(
            includeClosed,
            cancellationToken);

        return accounts
            .Select(Map)
            .ToList();
    }

    public async Task<AccountDto?> RenameAsync(
        Guid id,
        string newName,
        CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdForUpdateAsync(
            id,
            cancellationToken);

        if (account is null)
        {
            return null;
        }

        account.Rename(newName);

        await _accountRepository.SaveChangesAsync(cancellationToken);

        return Map(account);
    }

    public async Task<AccountDto?> CloseAsync(
        Guid id,
        string reason,
        AccountStatusChangeSource source,
        CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdForUpdateAsync(
            id,
            cancellationToken);

        if (account is null)
        {
            return null;
        }

        account.Close(reason, source);

        await _accountRepository.SaveChangesAsync(cancellationToken);

        return Map(account);
    }

    public async Task<AccountDto?> ReopenAsync(
        Guid id,
        string reason,
        AccountStatusChangeSource source,
        CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdForUpdateAsync(
            id,
            cancellationToken);

        if (account is null)
        {
            return null;
        }

        account.Reopen(reason, source);

        await _accountRepository.SaveChangesAsync(cancellationToken);

        return Map(account);
    }

    public async Task<IReadOnlyList<AccountStatusChangeDto>?> GetStatusHistoryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (account is null)
        {
            return null;
        }

        var statusChanges = await _accountRepository.GetStatusHistoryAsync(
            id,
            cancellationToken);

        return statusChanges
            .Select(MapStatusChange)
            .ToList();
    }

    private static AccountDto Map(Account account)
    {
        return new AccountDto(
            account.Id,
            account.Name,
            account.Type,
            account.Currency,
            account.Status,
            account.Balance,
            account.BalanceAsOfUtc,
            account.CreatedAtUtc,
            account.ClosedAtUtc);
    }

    private static AccountStatusChangeDto MapStatusChange(
        AccountStatusChange statusChange)
    {
        return new AccountStatusChangeDto(
            statusChange.Id,
            statusChange.AccountId,
            statusChange.FromStatus,
            statusChange.ToStatus,
            statusChange.Reason,
            statusChange.Source,
            statusChange.ChangedAtUtc);
    }
}
