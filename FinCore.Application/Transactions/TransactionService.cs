using FinCore.Application.Accounts;
using FinCore.Application.Common.Pagination;
using FinCore.Application.Common.Persistence;
using FinCore.Domain.Transactions;

namespace FinCore.Application.Transactions;

public sealed class TransactionService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TransactionService(
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TransactionDto?> CreateAsync(
        CreateTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        var account =
            await _accountRepository.GetByIdForUpdateAsync(
                command.AccountId,
                cancellationToken);

        if (account is null)
        {
            return null;
        }

        var transaction = Transaction.Create(
            command.AccountId,
            command.Amount,
            command.Description,
            command.OccurredAtUtc);

        account.ApplyTransaction(
            transaction.Amount,
            transaction.CreatedAtUtc);

        await _transactionRepository.AddAsync(
            transaction,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Map(transaction);
    }

    public async Task<TransactionDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var transaction =
            await _transactionRepository.GetByIdAsync(
                id,
                cancellationToken);

        return transaction is null
            ? null
            : Map(transaction);
    }

    public async Task<PagedResult<TransactionDto>> GetByAccountIdAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException(
                "Account ID is required.",
                nameof(accountId));
        }

        if (page < 1)
        {
            throw new ArgumentException(
                "Page must be at least 1.",
                nameof(page));
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentException(
                "Page size must be between 1 and 100.",
                nameof(pageSize));
        }

        var skip = (page - 1) * pageSize;

        var transactions =
            await _transactionRepository.GetByAccountIdAsync(
                accountId,
                skip,
                pageSize,
                cancellationToken);

        var totalCount =
            await _transactionRepository.CountByAccountIdAsync(
                accountId,
                cancellationToken);

        return new PagedResult<TransactionDto>(
            transactions
                .Select(Map)
                .ToList(),
            page,
            pageSize,
            totalCount);
    }

    private static TransactionDto Map(
        Transaction transaction)
    {
        return new TransactionDto(
            transaction.Id,
            transaction.AccountId,
            transaction.Amount,
            transaction.Description,
            transaction.OccurredAtUtc,
            transaction.CreatedAtUtc);
    }
}
