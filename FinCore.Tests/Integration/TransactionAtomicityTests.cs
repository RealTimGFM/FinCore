using FinCore.Application.Common;
using FinCore.Domain.Accounts;
using FinCore.Domain.Transactions;
using FinCore.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Tests.Integration;

public sealed class TransactionAtomicityTests
{
    [SqlServerFact]
    public async Task ConcurrencyConflict_RollsBackTransactionAndBalanceChange()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable(
                "FINCORE_TEST_SQLSERVER")!;

        var connectionStringBuilder =
            new SqlConnectionStringBuilder(
                baseConnectionString)
            {
                InitialCatalog =
                    $"FinCore_AtomicityTest_{Guid.NewGuid():N}"
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

            await using (var setupContext =
                         new FinCoreDbContext(options))
            {
                await setupContext.Database.MigrateAsync();

                var account = Account.Create(
                    "TD Chequing",
                    AccountType.Chequing,
                    "CAD");

                setupContext.Accounts.Add(account);

                await setupContext.SaveChangesAsync();

                accountId = account.Id;
            }

            await using var contextA =
                new FinCoreDbContext(options);

            await using var contextB =
                new FinCoreDbContext(options);

            var accountA =
                await contextA.Accounts
                    .SingleAsync(
                        account => account.Id == accountId);

            var accountB =
                await contextB.Accounts
                    .SingleAsync(
                        account => account.Id == accountId);

            // Context B changes the account first.
            accountB.Rename("Changed elsewhere");

            await contextB.SaveChangesAsync();

            // Context A is now stale.
            var transaction = Transaction.Create(
                accountId,
                -25m,
                "Groceries",
                DateTimeOffset.UtcNow);

            accountA.ApplyTransaction(
                transaction.Amount,
                transaction.CreatedAtUtc);

            contextA.Transactions.Add(transaction);

            var unitOfWorkA =
                new EfUnitOfWork(contextA);

            await Assert.ThrowsAsync<
                ConcurrencyConflictException>(
                () => unitOfWorkA.SaveChangesAsync());

            await using var verificationContext =
                new FinCoreDbContext(options);

            var savedAccount =
                await verificationContext.Accounts
                    .AsNoTracking()
                    .SingleAsync(
                        account => account.Id == accountId);

            var transactionExists =
                await verificationContext.Transactions
                    .AsNoTracking()
                    .AnyAsync(
                        item => item.Id == transaction.Id);

            Assert.Equal(
                "Changed elsewhere",
                savedAccount.Name);

            Assert.Equal(
                0m,
                savedAccount.Balance);

            Assert.False(transactionExists);
        }
        finally
        {
            await using var cleanupContext =
                new FinCoreDbContext(options);

            await cleanupContext.Database
                .EnsureDeletedAsync();
        }
    }
}
