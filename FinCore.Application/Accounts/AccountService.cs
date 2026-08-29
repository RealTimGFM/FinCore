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
        CancellationToken cancellationToken = default)
    {
        var accounts = await _accountRepository.GetAllAsync(
            cancellationToken);

        return accounts
            .Select(Map)
            .ToList();
    }

    private static AccountDto Map(Account account)
    {
        return new AccountDto(
            account.Id,
            account.Name,
            account.Type,
            account.Currency,
            account.Balance,
            account.BalanceAsOfUtc,
            account.CreatedAtUtc);
    }
}