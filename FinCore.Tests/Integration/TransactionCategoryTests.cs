using FinCore.Application.Transactions;
using FinCore.Domain.Accounts;
using FinCore.Domain.Categories;
using FinCore.Domain.Transactions;
using FinCore.Infrastructure.Accounts;
using FinCore.Infrastructure.Categories;
using FinCore.Infrastructure.Persistence;
using FinCore.Infrastructure.Transactions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Tests.Integration;

public sealed class TransactionCategoryTests
{
    [SqlServerFact]
    public async Task AssignCategory_PersistsAndDoesNotChangeBalance()
    {
        var options = CreateOptions("Assign");

        try
        {
            Guid transactionId;
            Guid categoryId;
            decimal balanceBeforeCategorization;

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

                var transaction = Transaction.Create(
                    account.Id,
                    -25m,
                    "McDonald's",
                    DateTimeOffset.UtcNow);

                account.ApplyTransaction(
                    transaction.Amount,
                    transaction.CreatedAtUtc);

                var category =
                    Category.Create("Restaurants");

                setupContext.Accounts.Add(account);
                setupContext.Transactions.Add(transaction);
                setupContext.Categories.Add(category);

                await setupContext.SaveChangesAsync();

                transactionId = transaction.Id;
                categoryId = category.Id;
                balanceBeforeCategorization = account.Balance;
            }

            await using (var actionContext =
                         new FinCoreDbContext(options))
            {
                var service =
                    CreateTransactionService(actionContext);

                var result =
                    await service.SetCategoryAsync(
                        transactionId,
                        categoryId);

                Assert.NotNull(result);
                Assert.Equal(
                    categoryId,
                    result.CategoryId);
            }

            await using var verificationContext =
                new FinCoreDbContext(options);

            var savedTransaction =
                await verificationContext.Transactions
                    .AsNoTracking()
                    .SingleAsync(
                        transaction =>
                            transaction.Id == transactionId);

            var savedBalance =
                await verificationContext.Accounts
                    .AsNoTracking()
                    .Select(account => account.Balance)
                    .SingleAsync();

            Assert.Equal(
                categoryId,
                savedTransaction.CategoryId);

            Assert.Equal(
                balanceBeforeCategorization,
                savedBalance);
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [SqlServerFact]
    public async Task UncategorizeTransaction_SetsCategoryToNullAndDoesNotChangeBalance()
    {
        var options = CreateOptions("Uncategorize");

        try
        {
            Guid transactionId;
            decimal balanceBeforeCategorization;

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

                var category =
                    Category.Create("Restaurants");

                var transaction = Transaction.Create(
                    account.Id,
                    -25m,
                    "McDonald's",
                    DateTimeOffset.UtcNow);

                transaction.SetCategory(category.Id);

                account.ApplyTransaction(
                    transaction.Amount,
                    transaction.CreatedAtUtc);

                setupContext.Accounts.Add(account);
                setupContext.Categories.Add(category);
                setupContext.Transactions.Add(transaction);

                await setupContext.SaveChangesAsync();

                transactionId = transaction.Id;
                balanceBeforeCategorization = account.Balance;
            }

            await using (var actionContext =
                         new FinCoreDbContext(options))
            {
                var service =
                    CreateTransactionService(actionContext);

                var result =
                    await service.SetCategoryAsync(
                        transactionId,
                        null);

                Assert.NotNull(result);
                Assert.Null(result.CategoryId);
            }

            await using var verificationContext =
                new FinCoreDbContext(options);

            var savedTransaction =
                await verificationContext.Transactions
                    .AsNoTracking()
                    .SingleAsync(
                        transaction =>
                            transaction.Id == transactionId);

            var savedBalance =
                await verificationContext.Accounts
                    .AsNoTracking()
                    .Select(account => account.Balance)
                    .SingleAsync();

            Assert.Null(savedTransaction.CategoryId);

            Assert.Equal(
                balanceBeforeCategorization,
                savedBalance);
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [SqlServerFact]
    public async Task AssignArchivedCategory_IsRejectedAndKeepsOriginalCategory()
    {
        var options = CreateOptions("Archived");

        try
        {
            Guid transactionId;
            Guid originalCategoryId;
            Guid archivedCategoryId;

            await using (var setupContext =
                         new FinCoreDbContext(options))
            {
                await setupContext.Database.MigrateAsync();

                var account = Account.Create(
                    "TD Chequing",
                    AccountType.Chequing,
                    "CAD");

                var originalCategory =
                    Category.Create("Restaurants");

                var archivedCategory =
                    Category.Create("Old Food");

                archivedCategory.Archive();

                var transaction = Transaction.Create(
                    account.Id,
                    -25m,
                    "McDonald's",
                    DateTimeOffset.UtcNow);

                transaction.SetCategory(
                    originalCategory.Id);

                account.ApplyTransaction(
                    transaction.Amount,
                    transaction.CreatedAtUtc);

                setupContext.Accounts.Add(account);
                setupContext.Categories.AddRange(
                    originalCategory,
                    archivedCategory);
                setupContext.Transactions.Add(transaction);

                await setupContext.SaveChangesAsync();

                transactionId = transaction.Id;
                originalCategoryId = originalCategory.Id;
                archivedCategoryId = archivedCategory.Id;
            }

            await using (var actionContext =
                         new FinCoreDbContext(options))
            {
                var service =
                    CreateTransactionService(actionContext);

                var exception =
                    await Assert.ThrowsAsync<
                        InvalidOperationException>(
                        () => service.SetCategoryAsync(
                            transactionId,
                            archivedCategoryId));

                Assert.Equal(
                    "An archived category cannot be assigned to a transaction.",
                    exception.Message);
            }

            await using var verificationContext =
                new FinCoreDbContext(options);

            var savedTransaction =
                await verificationContext.Transactions
                    .AsNoTracking()
                    .SingleAsync(
                        transaction =>
                            transaction.Id == transactionId);

            Assert.Equal(
                originalCategoryId,
                savedTransaction.CategoryId);
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [SqlServerFact]
    public async Task ReverseCategorizedTransaction_InheritsCategory()
    {
        var options = CreateOptions("Reversal");

        try
        {
            Guid originalTransactionId;
            Guid categoryId;

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

                var category =
                    Category.Create("Restaurants");

                var transaction = Transaction.Create(
                    account.Id,
                    -25m,
                    "McDonald's",
                    DateTimeOffset.UtcNow);

                transaction.SetCategory(category.Id);

                account.ApplyTransaction(
                    transaction.Amount,
                    transaction.CreatedAtUtc);

                setupContext.Accounts.Add(account);
                setupContext.Categories.Add(category);
                setupContext.Transactions.Add(transaction);

                await setupContext.SaveChangesAsync();

                originalTransactionId =
                    transaction.Id;

                categoryId =
                    category.Id;
            }

            Guid reversalId;

            await using (var actionContext =
                         new FinCoreDbContext(options))
            {
                var service =
                    CreateTransactionService(actionContext);

                var reversal =
                    await service.ReverseAsync(
                        new ReverseTransactionCommand(
                            originalTransactionId,
                            "Purchase reversed",
                            DateTimeOffset.UtcNow));

                Assert.NotNull(reversal);

                Assert.Equal(
                    categoryId,
                    reversal.CategoryId);

                reversalId = reversal.Id;
            }

            await using var verificationContext =
                new FinCoreDbContext(options);

            var original =
                await verificationContext.Transactions
                    .AsNoTracking()
                    .SingleAsync(
                        transaction =>
                            transaction.Id ==
                            originalTransactionId);

            var reversalTransaction =
                await verificationContext.Transactions
                    .AsNoTracking()
                    .SingleAsync(
                        transaction =>
                            transaction.Id ==
                            reversalId);

            var balance =
                await verificationContext.Accounts
                    .AsNoTracking()
                    .Select(account => account.Balance)
                    .SingleAsync();

            Assert.Equal(
                categoryId,
                original.CategoryId);

            Assert.Equal(
                categoryId,
                reversalTransaction.CategoryId);

            Assert.Equal(
                originalTransactionId,
                reversalTransaction.ReversalOfTransactionId);

            Assert.Equal(
                1000m,
                balance);
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
                    $"FinCore_Category_{testName}_{Guid.NewGuid():N}"
            };

        return new DbContextOptionsBuilder<FinCoreDbContext>()
            .UseSqlServer(
                connectionStringBuilder.ConnectionString)
            .Options;
    }

    private static async Task DeleteDatabaseAsync(
        DbContextOptions<FinCoreDbContext> options)
    {
        await using var context =
            new FinCoreDbContext(options);

        await context.Database.EnsureDeletedAsync();
    }
}
