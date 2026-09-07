using FinCore.Application.Common;
using FinCore.Domain.Accounts;
using FinCore.Infrastructure.Accounts;
using FinCore.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Tests.Integration;

public sealed class AccountConcurrencyTests
{
    [SqlServerFact]
    public async Task TwoConcurrentUpdates_SecondUpdateIsRejected()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable(
                "FINCORE_TEST_SQLSERVER")!;

        var connectionStringBuilder =
            new SqlConnectionStringBuilder(
                baseConnectionString)
            {
                InitialCatalog =
                    $"FinCore_ConcurrencyTest_{Guid.NewGuid():N}"
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

            var repositoryA =
                new AccountRepository(contextA);

            var repositoryB =
                new AccountRepository(contextB);

            var unitOfWorkA =
                new EfUnitOfWork(contextA);

            var unitOfWorkB =
                new EfUnitOfWork(contextB);

            var accountA =
                await repositoryA.GetByIdForUpdateAsync(
                    accountId);

            var accountB =
                await repositoryB.GetByIdForUpdateAsync(
                    accountId);

            Assert.NotNull(accountA);
            Assert.NotNull(accountB);

            Assert.True(
                accountA.RowVersion.SequenceEqual(
                    accountB.RowVersion));

            accountA.Rename("Changed by request A");
            accountB.Rename("Changed by request B");

            await unitOfWorkA.SaveChangesAsync();

            var exception =
                await Assert.ThrowsAsync<
                    ConcurrencyConflictException>(
                    () => unitOfWorkB.SaveChangesAsync());

            Assert.IsType<DbUpdateConcurrencyException>(
                exception.InnerException);

            await using var verificationContext =
                new FinCoreDbContext(options);

            var savedAccount =
                await verificationContext.Accounts
                    .AsNoTracking()
                    .SingleAsync(
                        account => account.Id == accountId);

            Assert.Equal(
                "Changed by request A",
                savedAccount.Name);
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
