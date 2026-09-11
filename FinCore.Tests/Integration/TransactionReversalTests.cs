using FinCore.Application.Accounts;
using FinCore.Application.Common;
using FinCore.Application.Common.Persistence;
using FinCore.Application.Transactions;
using FinCore.Domain.Accounts;
using FinCore.Domain.Transactions;
using FinCore.Infrastructure.Accounts;
using FinCore.Infrastructure.Categories;
using FinCore.Infrastructure.Persistence;
using FinCore.Infrastructure.Transactions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Tests.Integration;

public sealed class TransactionReversalTests
{
    [SqlServerFact]
    public async Task ReverseTransaction_RestoresBalanceAndPreservesHistory()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable(
                "FINCORE_TEST_SQLSERVER")!;

        var connectionStringBuilder =
            new SqlConnectionStringBuilder(
                baseConnectionString)
            {
                InitialCatalog =
                    $"FinCore_ReversalTest_{Guid.NewGuid():N}"
            };

        var connectionString =
            connectionStringBuilder.ConnectionString;

        var options =
            new DbContextOptionsBuilder<FinCoreDbContext>()
                .UseSqlServer(connectionString)
                .Options;

        try
        {
            Guid accountId;
            Guid originalTransactionId;

            await using (var setupContext =
                         new FinCoreDbContext(options))
            {
                await setupContext.Database.MigrateAsync();

                var account = Account.Create(
                    "TD Chequing",
                    AccountType.Chequing,
                    "CAD");

                account.RefreshBalanceSnapshot(
                    1000m,
                    DateTimeOffset.UtcNow);

                setupContext.Accounts.Add(account);

                var originalTransaction =
                    Transaction.Create(
                        account.Id,
                        -250m,
                        "Groceries",
                        DateTimeOffset.UtcNow);

                account.ApplyTransaction(
                    originalTransaction.Amount,
                    originalTransaction.CreatedAtUtc);

                setupContext.Transactions.Add(
                    originalTransaction);

                await setupContext.SaveChangesAsync();

                accountId = account.Id;
                originalTransactionId =
                    originalTransaction.Id;
            }

            await using (var reversalContext =
                         new FinCoreDbContext(options))
            {
                var accountRepository =
                    new AccountRepository(
                        reversalContext);

                var transactionRepository =
                    new TransactionRepository(
                        reversalContext);

                var unitOfWork =
                    new EfUnitOfWork(
                        reversalContext);

                var service =
                    new TransactionService(
                        accountRepository,
                        transactionRepository,
                        new CategoryRepository(reversalContext),
                        unitOfWork);

                var reversal =
                    await service.ReverseAsync(
                        new ReverseTransactionCommand(
                            originalTransactionId,
                            "Entered wrong amount",
                            DateTimeOffset.UtcNow));

                Assert.NotNull(reversal);

                Assert.Equal(
                    250m,
                    reversal.Amount);

                Assert.True(
                    reversal.IsReversal);

                Assert.Equal(
                    originalTransactionId,
                    reversal.ReversalOfTransactionId);
            }

            await using var verificationContext =
                new FinCoreDbContext(options);

            var savedAccount =
                await verificationContext.Accounts
                    .AsNoTracking()
                    .SingleAsync(
                        account =>
                            account.Id == accountId);

            var transactions =
                await verificationContext.Transactions
                    .AsNoTracking()
                    .Where(transaction =>
                        transaction.AccountId == accountId)
                    .ToListAsync();

            Assert.Equal(
                1000m,
                savedAccount.Balance);

            Assert.Equal(
                2,
                transactions.Count);

            var original =
                transactions.Single(
                    transaction =>
                        transaction.Id ==
                        originalTransactionId);

            var reversalTransaction =
                transactions.Single(
                    transaction =>
                        transaction.ReversalOfTransactionId ==
                        originalTransactionId);

            Assert.Equal(
                -250m,
                original.Amount);

            Assert.Equal(
                250m,
                reversalTransaction.Amount);
        }
        finally
        {
            await using var cleanupContext =
                new FinCoreDbContext(options);

            await cleanupContext.Database
                .EnsureDeletedAsync();
        }
    }

    [SqlServerFact]
    public async Task ReverseTransaction_Twice_SecondAttemptIsRejected()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable(
                "FINCORE_TEST_SQLSERVER")!;

        var connectionStringBuilder =
            new SqlConnectionStringBuilder(
                baseConnectionString)
            {
                InitialCatalog =
                    $"FinCore_DoubleReversalTest_{Guid.NewGuid():N}"
            };

        var connectionString =
            connectionStringBuilder.ConnectionString;

        var options =
            new DbContextOptionsBuilder<FinCoreDbContext>()
                .UseSqlServer(connectionString)
                .Options;

        try
        {
            Guid accountId;
            Guid originalTransactionId;

            await using (var setupContext =
                         new FinCoreDbContext(options))
            {
                await setupContext.Database.MigrateAsync();

                var account = Account.Create(
                    "TD Chequing",
                    AccountType.Chequing,
                    "CAD");

                var original =
                    Transaction.Create(
                        account.Id,
                        -100m,
                        "Groceries",
                        DateTimeOffset.UtcNow);

                account.ApplyTransaction(
                    original.Amount,
                    original.CreatedAtUtc);

                setupContext.Accounts.Add(account);
                setupContext.Transactions.Add(original);

                await setupContext.SaveChangesAsync();

                accountId = account.Id;
                originalTransactionId = original.Id;
            }

            await using (var firstContext =
                         new FinCoreDbContext(options))
            {
                var service =
                    CreateTransactionService(
                        firstContext);

                await service.ReverseAsync(
                    new ReverseTransactionCommand(
                        originalTransactionId,
                        "First reversal",
                        DateTimeOffset.UtcNow));
            }

            await using (var secondContext =
                         new FinCoreDbContext(options))
            {
                var service =
                    CreateTransactionService(
                        secondContext);

                var exception =
                    await Assert.ThrowsAsync<
                        InvalidOperationException>(
                        () => service.ReverseAsync(
                            new ReverseTransactionCommand(
                                originalTransactionId,
                                "Second reversal",
                                DateTimeOffset.UtcNow)));

                Assert.Equal(
                    "This transaction has already been reversed.",
                    exception.Message);
            }

            await using var verificationContext =
                new FinCoreDbContext(options);

            var reversalCount =
                await verificationContext.Transactions
                    .CountAsync(transaction =>
                        transaction.ReversalOfTransactionId ==
                        originalTransactionId);

            var savedTransactions =
                await verificationContext.Transactions
                    .AsNoTracking()
                    .Where(transaction =>
                        transaction.AccountId == accountId)
                    .ToListAsync();

            var savedAccount =
                await verificationContext.Accounts
                    .AsNoTracking()
                    .SingleAsync(account =>
                        account.Id == accountId);

            Assert.Equal(
                1,
                reversalCount);

            Assert.Equal(2, savedTransactions.Count);
            Assert.Contains(
                savedTransactions,
                transaction => transaction.Id == originalTransactionId);
            Assert.Single(
                savedTransactions,
                transaction =>
                    transaction.ReversalOfTransactionId ==
                    originalTransactionId);

            Assert.Equal(
                0m,
                savedAccount.Balance);
        }
        finally
        {
            await using var cleanupContext =
                new FinCoreDbContext(options);

            await cleanupContext.Database
                .EnsureDeletedAsync();
        }
    }

    [SqlServerFact]
    public async Task ReverseTransaction_ReversalTransaction_IsRejected()
    {
        var options = CreateOptions("ReverseAReversal");

        try
        {
            var (accountId, originalTransactionId) =
                await CreateAccountAndOriginalAsync(options, -100m);

            Guid reversalTransactionId;

            await using (var firstContext = new FinCoreDbContext(options))
            {
                var reversal = await CreateTransactionService(firstContext)
                    .ReverseAsync(new ReverseTransactionCommand(
                        originalTransactionId,
                        "Correct original",
                        DateTimeOffset.UtcNow));

                Assert.NotNull(reversal);
                reversalTransactionId = reversal.Id;
            }

            await using (var secondContext = new FinCoreDbContext(options))
            {
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => CreateTransactionService(secondContext)
                        .ReverseAsync(new ReverseTransactionCommand(
                            reversalTransactionId,
                            "Try to reverse reversal",
                            DateTimeOffset.UtcNow)));

                Assert.Equal(
                    "A reversal transaction cannot itself be reversed.",
                    exception.Message);
            }

            await using var verificationContext = new FinCoreDbContext(options);
            var transactions = await verificationContext.Transactions
                .AsNoTracking()
                .Where(transaction => transaction.AccountId == accountId)
                .ToListAsync();
            var savedAccount = await verificationContext.Accounts
                .AsNoTracking()
                .SingleAsync(item => item.Id == accountId);

            Assert.Equal(2, transactions.Count);
            Assert.Contains(transactions, item => item.Id == originalTransactionId);
            Assert.Contains(transactions, item => item.Id == reversalTransactionId);
            Assert.Equal(0m, savedAccount.Balance);
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [SqlServerFact]
    public async Task ReverseTransaction_ClosedAccount_IsRejectedWithoutMutation()
    {
        var options = CreateOptions("ClosedAccountReversal");

        try
        {
            var (accountId, originalTransactionId) =
                await CreateAccountAndOriginalAsync(options, -100m);

            await using (var closeContext = new FinCoreDbContext(options))
            {
                var accountToClose = await closeContext.Accounts
                    .SingleAsync(item => item.Id == accountId);
                accountToClose.Close(
                    "Account no longer used",
                    AccountStatusChangeSource.User);
                await closeContext.SaveChangesAsync();
            }

            await using (var reversalContext = new FinCoreDbContext(options))
            {
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => CreateTransactionService(reversalContext)
                        .ReverseAsync(new ReverseTransactionCommand(
                            originalTransactionId,
                            "Reverse after close",
                            DateTimeOffset.UtcNow)));

                Assert.Equal(
                    "Transactions cannot be added to a closed account.",
                    exception.Message);
            }

            await using var verificationContext = new FinCoreDbContext(options);
            var savedAccount = await verificationContext.Accounts
                .AsNoTracking()
                .SingleAsync(item => item.Id == accountId);
            var transactions = await verificationContext.Transactions
                .AsNoTracking()
                .Where(item => item.AccountId == accountId)
                .ToListAsync();

            Assert.Equal(-100m, savedAccount.Balance);
            Assert.Single(transactions);
            Assert.Equal(originalTransactionId, transactions[0].Id);
            Assert.Null(transactions[0].ReversalOfTransactionId);
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [SqlServerFact]
    public async Task ReversalConcurrencyConflict_RollsBackReversalAndBalanceChange()
    {
        var options = CreateOptions("ReversalAtomicity");

        try
        {
            var (accountId, originalTransactionId) =
                await CreateAccountAndOriginalAsync(options, -100m);

            await using var staleContext = new FinCoreDbContext(options);
            await using var concurrentContext = new FinCoreDbContext(options);

            var staleAccount = await staleContext.Accounts
                .SingleAsync(item => item.Id == accountId);
            var original = await staleContext.Transactions
                .AsNoTracking()
                .SingleAsync(item => item.Id == originalTransactionId);

            var reversal = Transaction.CreateReversal(
                original,
                "Stale reversal",
                DateTimeOffset.UtcNow);
            staleAccount.ApplyTransaction(
                reversal.Amount,
                reversal.CreatedAtUtc);
            staleContext.Transactions.Add(reversal);

            var concurrentAccount = await concurrentContext.Accounts
                .SingleAsync(item => item.Id == accountId);
            concurrentAccount.Rename("Changed by another request");
            await concurrentContext.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<ConcurrencyConflictException>(
                () => new EfUnitOfWork(staleContext).SaveChangesAsync());

            Assert.IsType<DbUpdateConcurrencyException>(exception.InnerException);

            await using var verificationContext = new FinCoreDbContext(options);
            var savedAccount = await verificationContext.Accounts
                .AsNoTracking()
                .SingleAsync(item => item.Id == accountId);
            var savedTransactions = await verificationContext.Transactions
                .AsNoTracking()
                .Where(item => item.AccountId == accountId)
                .ToListAsync();

            Assert.Equal("Changed by another request", savedAccount.Name);
            Assert.Equal(-100m, savedAccount.Balance);
            Assert.Single(savedTransactions);
            Assert.Equal(originalTransactionId, savedTransactions[0].Id);
            Assert.DoesNotContain(savedTransactions, item => item.Id == reversal.Id);
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [SqlServerFact]
    public async Task SimultaneousDoubleReversal_OnlyOneCommits()
    {
        var options = CreateOptions("SimultaneousDoubleReversal");

        try
        {
            var (accountId, originalTransactionId) =
                await CreateAccountAndOriginalAsync(options, -100m);

            await using var winnerContext = new FinCoreDbContext(options);
            await using var loserContext = new FinCoreDbContext(options);
            using var bothChecked = new Barrier(2);
            using var bothLoadedAccounts = new Barrier(2);
            var winnerCommitted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var winnerService = new TransactionService(
                new CoordinatedAccountRepository(
                    new AccountRepository(winnerContext),
                    bothLoadedAccounts),
                new CoordinatedTransactionRepository(
                    new TransactionRepository(winnerContext),
                    bothChecked),
                new CategoryRepository(winnerContext),
                new SignalingUnitOfWork(
                    new EfUnitOfWork(winnerContext),
                    winnerCommitted));

            var loserService = new TransactionService(
                new CoordinatedAccountRepository(
                    new AccountRepository(loserContext),
                    bothLoadedAccounts),
                new CoordinatedTransactionRepository(
                    new TransactionRepository(loserContext),
                    bothChecked),
                new CategoryRepository(loserContext),
                new WaitingUnitOfWork(
                    new EfUnitOfWork(loserContext),
                    winnerCommitted.Task));

            var loserTask = loserService.ReverseAsync(
                new ReverseTransactionCommand(
                    originalTransactionId,
                    "Losing reversal",
                    DateTimeOffset.UtcNow));
            var winnerTask = winnerService.ReverseAsync(
                new ReverseTransactionCommand(
                    originalTransactionId,
                    "Winning reversal",
                    DateTimeOffset.UtcNow));

            var winner = await winnerTask;
            var loserException = await Assert.ThrowsAsync<ConcurrencyConflictException>(
                () => loserTask);

            Assert.NotNull(winner);
            Assert.Equal(
                "The data was changed by another request. Reload it and try again.",
                loserException.Message);
            Assert.IsType<DbUpdateConcurrencyException>(
                loserException.InnerException);

            await using var verificationContext = new FinCoreDbContext(options);
            var savedAccount = await verificationContext.Accounts
                .AsNoTracking()
                .SingleAsync(item => item.Id == accountId);
            var transactions = await verificationContext.Transactions
                .AsNoTracking()
                .Where(item => item.AccountId == accountId)
                .ToListAsync();

            Assert.Equal(0m, savedAccount.Balance);
            Assert.Equal(2, transactions.Count);
            Assert.Contains(transactions, item => item.Id == originalTransactionId);
            Assert.Single(
                transactions,
                item => item.ReversalOfTransactionId == originalTransactionId);
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [SqlServerFact]
    public async Task DoubleReversalRace_UniqueIndexLoserIsCleanConflict()
    {
        var options = CreateOptions("UniqueIndexDoubleReversal");

        try
        {
            var (accountId, originalTransactionId) =
                await CreateAccountAndOriginalAsync(options, -100m);

            await using var winnerContext = new FinCoreDbContext(options);
            await using var loserContext = new FinCoreDbContext(options);
            using var bothChecked = new Barrier(2);
            var winnerCommitted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var winnerService = new TransactionService(
                new AccountRepository(winnerContext),
                new CoordinatedTransactionRepository(
                    new TransactionRepository(winnerContext),
                    bothChecked),
                new CategoryRepository(winnerContext),
                new SignalingUnitOfWork(
                    new EfUnitOfWork(winnerContext),
                    winnerCommitted));

            var loserService = new TransactionService(
                new WaitingAccountRepository(
                    new AccountRepository(loserContext),
                    winnerCommitted.Task),
                new CoordinatedTransactionRepository(
                    new TransactionRepository(loserContext),
                    bothChecked),
                new CategoryRepository(loserContext),
                new EfUnitOfWork(loserContext));

            var loserTask = loserService.ReverseAsync(
                new ReverseTransactionCommand(
                    originalTransactionId,
                    "Losing unique-index reversal",
                    DateTimeOffset.UtcNow));
            var winnerTask = winnerService.ReverseAsync(
                new ReverseTransactionCommand(
                    originalTransactionId,
                    "Winning unique-index reversal",
                    DateTimeOffset.UtcNow));

            var winner = await winnerTask;
            var loserException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => loserTask);

            Assert.NotNull(winner);
            Assert.Equal(
                "This transaction has already been reversed.",
                loserException.Message);
            var dbUpdateException = Assert.IsType<DbUpdateException>(
                loserException.InnerException);
            var sqlException = Assert.IsType<SqlException>(
                dbUpdateException.InnerException);
            Assert.Contains(sqlException.Number, new[] { 2601, 2627 });

            await using var verificationContext = new FinCoreDbContext(options);
            var savedAccount = await verificationContext.Accounts
                .AsNoTracking()
                .SingleAsync(item => item.Id == accountId);
            var transactions = await verificationContext.Transactions
                .AsNoTracking()
                .Where(item => item.AccountId == accountId)
                .ToListAsync();

            Assert.Equal(0m, savedAccount.Balance);
            Assert.Equal(2, transactions.Count);
            Assert.Contains(transactions, item => item.Id == originalTransactionId);
            Assert.Single(
                transactions,
                item => item.ReversalOfTransactionId == originalTransactionId);
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [SqlServerFact]
    public async Task TransactionReversalSchema_HasRequiredDatabaseInvariants()
    {
        var options = CreateOptions("ReversalSchema");

        try
        {
            await using var context = new FinCoreDbContext(options);
            await context.Database.MigrateAsync();
            await context.Database.OpenConnectionAsync();

            Assert.Equal(1, await ExecuteCountAsync(context, """
                SELECT COUNT(*)
                FROM sys.columns AS c
                INNER JOIN sys.types AS t ON c.user_type_id = t.user_type_id
                WHERE c.object_id = OBJECT_ID(N'dbo.Transactions')
                  AND c.name = N'ReversalOfTransactionId'
                  AND t.name = N'uniqueidentifier'
                  AND c.is_nullable = 1;
                """));

            Assert.Equal(1, await ExecuteCountAsync(context, """
                SELECT COUNT(*)
                FROM sys.foreign_keys
                WHERE name = N'FK_Transactions_Transactions_ReversalOfTransactionId'
                  AND parent_object_id = OBJECT_ID(N'dbo.Transactions')
                  AND referenced_object_id = OBJECT_ID(N'dbo.Transactions')
                  AND delete_referential_action_desc = N'NO_ACTION';
                """));

            Assert.Equal(1, await ExecuteCountAsync(context, """
                SELECT COUNT(*)
                FROM sys.indexes
                WHERE object_id = OBJECT_ID(N'dbo.Transactions')
                  AND name = N'IX_Transactions_ReversalOfTransactionId'
                  AND is_unique = 1
                  AND has_filter = 1
                  AND CHARINDEX(N'ReversalOfTransactionId', filter_definition) > 0
                  AND CHARINDEX(N'IS NOT NULL', filter_definition) > 0;
                """));

            Assert.Equal(1, await ExecuteCountAsync(context, """
                SELECT COUNT(*)
                FROM sys.check_constraints
                WHERE parent_object_id = OBJECT_ID(N'dbo.Transactions')
                  AND name = N'CK_Transactions_Amount_NotZero'
                  AND is_disabled = 0;
                """));
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    private static TransactionService CreateTransactionService(
        FinCoreDbContext context)
    {
        return new TransactionService(
            new AccountRepository(context),
            new TransactionRepository(context),
            new CategoryRepository(context),
            new EfUnitOfWork(context));
    }

    private static DbContextOptions<FinCoreDbContext> CreateOptions(
        string testName)
    {
        var baseConnectionString = Environment.GetEnvironmentVariable(
            "FINCORE_TEST_SQLSERVER")!;
        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = $"FinCore_{testName}_{Guid.NewGuid():N}"
        };

        return new DbContextOptionsBuilder<FinCoreDbContext>()
            .UseSqlServer(builder.ConnectionString)
            .Options;
    }

    private static async Task<(Guid AccountId, Guid OriginalTransactionId)>
        CreateAccountAndOriginalAsync(
            DbContextOptions<FinCoreDbContext> options,
            decimal amount)
    {
        await using var context = new FinCoreDbContext(options);
        await context.Database.MigrateAsync();

        var account = Account.Create(
            "TD Chequing",
            AccountType.Chequing,
            "CAD");
        var original = Transaction.Create(
            account.Id,
            amount,
            "Original transaction",
            DateTimeOffset.UtcNow);
        account.ApplyTransaction(original.Amount, original.CreatedAtUtc);

        context.Accounts.Add(account);
        context.Transactions.Add(original);
        await context.SaveChangesAsync();

        return (account.Id, original.Id);
    }

    private static async Task DeleteDatabaseAsync(
        DbContextOptions<FinCoreDbContext> options)
    {
        await using var context = new FinCoreDbContext(options);
        await context.Database.EnsureDeletedAsync();
    }

    private static async Task<int> ExecuteCountAsync(
        FinCoreDbContext context,
        string sql)
    {
        await using var command = context.Database
            .GetDbConnection()
            .CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private sealed class CoordinatedTransactionRepository(
        ITransactionRepository inner,
        Barrier bothChecked)
        : ITransactionRepository
    {
        public Task<Transaction?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            inner.GetByIdAsync(id, cancellationToken);

        public Task<Transaction?> GetByIdForUpdateAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            inner.GetByIdForUpdateAsync(id, cancellationToken);

        public Task<IReadOnlyList<Transaction>> GetByAccountIdAsync(
            Guid accountId,
            int skip,
            int take,
            CancellationToken cancellationToken = default) =>
            inner.GetByAccountIdAsync(accountId, skip, take, cancellationToken);

        public Task<int> CountByAccountIdAsync(
            Guid accountId,
            CancellationToken cancellationToken = default) =>
            inner.CountByAccountIdAsync(accountId, cancellationToken);

        public async Task<bool> HasReversalAsync(
            Guid transactionId,
            CancellationToken cancellationToken = default)
        {
            var result = await inner.HasReversalAsync(
                transactionId,
                cancellationToken);
            bothChecked.SignalAndWait(cancellationToken);
            return result;
        }

        public Task AddAsync(
            Transaction transaction,
            CancellationToken cancellationToken = default) =>
            inner.AddAsync(transaction, cancellationToken);
    }

    private sealed class CoordinatedAccountRepository(
        IAccountRepository inner,
        Barrier bothLoadedAccounts)
        : IAccountRepository
    {
        public Task<Account?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            inner.GetByIdAsync(id, cancellationToken);

        public async Task<Account?> GetByIdForUpdateAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var account = await inner.GetByIdForUpdateAsync(id, cancellationToken);
            bothLoadedAccounts.SignalAndWait(cancellationToken);
            return account;
        }

        public Task<IReadOnlyList<Account>> GetAllAsync(
            bool includeClosed,
            CancellationToken cancellationToken = default) =>
            inner.GetAllAsync(includeClosed, cancellationToken);

        public Task<IReadOnlyList<AccountStatusChange>> GetStatusHistoryAsync(
            Guid accountId,
            CancellationToken cancellationToken = default) =>
            inner.GetStatusHistoryAsync(accountId, cancellationToken);

        public Task AddAsync(
            Account account,
            CancellationToken cancellationToken = default) =>
            inner.AddAsync(account, cancellationToken);
    }

    private sealed class WaitingAccountRepository(
        IAccountRepository inner,
        Task winnerCommitted)
        : IAccountRepository
    {
        public Task<Account?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            inner.GetByIdAsync(id, cancellationToken);

        public async Task<Account?> GetByIdForUpdateAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            await winnerCommitted.WaitAsync(cancellationToken);
            return await inner.GetByIdForUpdateAsync(id, cancellationToken);
        }

        public Task<IReadOnlyList<Account>> GetAllAsync(
            bool includeClosed,
            CancellationToken cancellationToken = default) =>
            inner.GetAllAsync(includeClosed, cancellationToken);

        public Task<IReadOnlyList<AccountStatusChange>> GetStatusHistoryAsync(
            Guid accountId,
            CancellationToken cancellationToken = default) =>
            inner.GetStatusHistoryAsync(accountId, cancellationToken);

        public Task AddAsync(
            Account account,
            CancellationToken cancellationToken = default) =>
            inner.AddAsync(account, cancellationToken);
    }

    private sealed class SignalingUnitOfWork(
        IUnitOfWork inner,
        TaskCompletionSource committed)
        : IUnitOfWork
    {
        public async Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            await inner.SaveChangesAsync(cancellationToken);
            committed.TrySetResult();
        }
    }

    private sealed class WaitingUnitOfWork(
        IUnitOfWork inner,
        Task winnerCommitted)
        : IUnitOfWork
    {
        public async Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            await winnerCommitted.WaitAsync(cancellationToken);
            await inner.SaveChangesAsync(cancellationToken);
        }
    }
}
