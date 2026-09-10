using FinCore.Application.Accounts;
using FinCore.Application.Common;
using FinCore.Application.Transactions;
using FinCore.Domain.Accounts;
using FinCore.Infrastructure.Accounts;
using FinCore.Infrastructure.Idempotency;
using FinCore.Infrastructure.Persistence;
using FinCore.Infrastructure.Transactions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Tests.Integration;

public sealed class TransactionIdempotencyTests
{
    [SqlServerFact]
    public async Task SameRequestAndKey_ReplaysOriginalTransaction()
    {
        var options = CreateOptions("Replay");

        try
        {
            var accountId = await CreateAccountAsync(options);
            var command = CreateCommand(accountId);

            await using var context =
                new FinCoreDbContext(options);

            var idempotencyService =
                new IdempotencyService(context);

            var transactionService =
                CreateTransactionService(context);

            var first = await idempotencyService.ExecuteAsync<
                CreateTransactionCommand,
                TransactionDto>(
                "purchase-001",
                command,
                () => CreateRequiredAsync(
                    transactionService,
                    command),
                201);

            var replay = await idempotencyService.ExecuteAsync<
                CreateTransactionCommand,
                TransactionDto>(
                "purchase-001",
                command,
                () => CreateRequiredAsync(
                    transactionService,
                    command),
                201);

            Assert.False(first.WasReplayed);
            Assert.True(replay.WasReplayed);
            Assert.Equal(first.Value, replay.Value);
            Assert.Equal(201, replay.StatusCode);

            await using var verificationContext =
                new FinCoreDbContext(options);

            Assert.Equal(
                1,
                await verificationContext.Transactions.CountAsync());

            Assert.Equal(
                1,
                await verificationContext.IdempotencyRecords.CountAsync());

            Assert.Equal(
                command.Amount,
                await verificationContext.Accounts
                    .Select(account => account.Balance)
                    .SingleAsync());
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [SqlServerFact]
    public async Task SameKeyAndDifferentRequest_IsRejected()
    {
        var options = CreateOptions("Reuse");

        try
        {
            var accountId = await CreateAccountAsync(options);
            var firstCommand = CreateCommand(accountId);

            var differentCommand =
                firstCommand with
                {
                    Amount = -1500m,
                    Description = "Best Buy"
                };

            await using var context =
                new FinCoreDbContext(options);

            var idempotencyService =
                new IdempotencyService(context);

            var transactionService =
                CreateTransactionService(context);

            await idempotencyService.ExecuteAsync<
                CreateTransactionCommand,
                TransactionDto>(
                "purchase-001",
                firstCommand,
                () => CreateRequiredAsync(
                    transactionService,
                    firstCommand),
                201);

            var exception =
                await Assert.ThrowsAsync<
                    IdempotencyKeyReuseException>(
                    () => idempotencyService.ExecuteAsync<
                        CreateTransactionCommand,
                        TransactionDto>(
                        "purchase-001",
                        differentCommand,
                        () => CreateRequiredAsync(
                            transactionService,
                            differentCommand),
                        201));

            Assert.Equal(
                "The idempotency key has already been used for a different request.",
                exception.Message);

            await using var verificationContext =
                new FinCoreDbContext(options);

            Assert.Equal(
                1,
                await verificationContext.Transactions.CountAsync());

            Assert.Equal(
                firstCommand.Amount,
                await verificationContext.Accounts
                    .Select(account => account.Balance)
                    .SingleAsync());
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [SqlServerFact]
    public async Task IdenticalRequestsWithDifferentKeys_CreateTwoTransactions()
    {
        var options = CreateOptions("DistinctKeys");

        try
        {
            var accountId = await CreateAccountAsync(options);
            var command = CreateCommand(accountId);

            await ExecuteCreateAsync(
                options,
                "purchase-001",
                command);

            await ExecuteCreateAsync(
                options,
                "purchase-002",
                command);

            await using var verificationContext =
                new FinCoreDbContext(options);

            Assert.Equal(
                2,
                await verificationContext.Transactions.CountAsync());

            Assert.Equal(
                2,
                await verificationContext.IdempotencyRecords.CountAsync());

            Assert.Equal(
                command.Amount * 2,
                await verificationContext.Accounts
                    .Select(account => account.Balance)
                    .SingleAsync());
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [SqlServerFact]
    public async Task FailedOperation_RollsBackFinancialChangesAndReservation()
    {
        var options = CreateOptions("Rollback");

        try
        {
            var accountId = await CreateAccountAsync(options);
            var command = CreateCommand(accountId);

            await using (var context =
                         new FinCoreDbContext(options))
            {
                var transactionService =
                    CreateTransactionService(context);

                var idempotencyService =
                    new IdempotencyService(context);

                async Task<TransactionDto> CreateThenFailAsync()
                {
                    await CreateRequiredAsync(
                        transactionService,
                        command);

                    throw new TestOperationException();
                }

                await Assert.ThrowsAsync<TestOperationException>(
                    () => idempotencyService.ExecuteAsync<
                        CreateTransactionCommand,
                        TransactionDto>(
                        "purchase-rollback",
                        command,
                        CreateThenFailAsync,
                        201));
            }

            await using var verificationContext =
                new FinCoreDbContext(options);

            Assert.Equal(
                0,
                await verificationContext.Transactions.CountAsync());

            Assert.Equal(
                0,
                await verificationContext.IdempotencyRecords.CountAsync());

            Assert.Equal(
                0m,
                await verificationContext.Accounts
                    .Select(account => account.Balance)
                    .SingleAsync());
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [SqlServerFact]
    public async Task SimultaneousRequestsWithSameKey_CreateOneTransaction()
    {
        var options = CreateOptions("Concurrent");

        try
        {
            var accountId = await CreateAccountAsync(options);
            var command = CreateCommand(accountId);

            var results = await Task.WhenAll(
                ExecuteCreateAsync(
                    options,
                    "simultaneous-purchase",
                    command),
                ExecuteCreateAsync(
                    options,
                    "simultaneous-purchase",
                    command));

            Assert.Single(
                results,
                result => !result.WasReplayed);

            Assert.Single(
                results,
                result => result.WasReplayed);

            Assert.Equal(
                results[0].Value.Id,
                results[1].Value.Id);

            await using var verificationContext =
                new FinCoreDbContext(options);

            Assert.Equal(
                1,
                await verificationContext.Transactions.CountAsync());

            Assert.Equal(
                1,
                await verificationContext.IdempotencyRecords.CountAsync());

            Assert.Equal(
                command.Amount,
                await verificationContext.Accounts
                    .Select(account => account.Balance)
                    .SingleAsync());
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    private static DbContextOptions<FinCoreDbContext>
        CreateOptions(string testName)
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable(
                "FINCORE_TEST_SQLSERVER")!;

        var connectionStringBuilder =
            new SqlConnectionStringBuilder(
                baseConnectionString)
            {
                InitialCatalog =
                    $"FinCore_Idempotency_{testName}_{Guid.NewGuid():N}"
            };

        return new DbContextOptionsBuilder<FinCoreDbContext>()
            .UseSqlServer(
                connectionStringBuilder.ConnectionString)
            .Options;
    }

    private static async Task<Guid> CreateAccountAsync(
        DbContextOptions<FinCoreDbContext> options)
    {
        await using var context =
            new FinCoreDbContext(options);

        await context.Database.MigrateAsync();

        var account = Account.Create(
            "TD Chequing",
            AccountType.Chequing,
            "CAD");

        context.Accounts.Add(account);

        await context.SaveChangesAsync();

        return account.Id;
    }

    private static CreateTransactionCommand CreateCommand(
        Guid accountId)
    {
        return new CreateTransactionCommand(
            accountId,
            -8.99m,
            "McDonald's",
            DateTimeOffset.UtcNow);
    }

    private static async Task<
        IdempotencyResult<TransactionDto>>
        ExecuteCreateAsync(
            DbContextOptions<FinCoreDbContext> options,
            string key,
            CreateTransactionCommand command)
    {
        await using var context =
            new FinCoreDbContext(options);

        var transactionService =
            CreateTransactionService(context);

        var idempotencyService =
            new IdempotencyService(context);

        return await idempotencyService.ExecuteAsync<
            CreateTransactionCommand,
            TransactionDto>(
            key,
            command,
            () => CreateRequiredAsync(
                transactionService,
                command),
            201);
    }

    private static TransactionService CreateTransactionService(
        FinCoreDbContext context)
    {
        return new TransactionService(
            new AccountRepository(context),
            new TransactionRepository(context),
            new EfUnitOfWork(context));
    }

    private static async Task<TransactionDto> CreateRequiredAsync(
        TransactionService service,
        CreateTransactionCommand command)
    {
        return await service.CreateAsync(command)
            ?? throw new InvalidOperationException(
                "Test account was not found.");
    }

    private static async Task DeleteDatabaseAsync(
        DbContextOptions<FinCoreDbContext> options)
    {
        await using var context =
            new FinCoreDbContext(options);

        await context.Database.EnsureDeletedAsync();
    }

    private sealed class TestOperationException
        : Exception;
}
