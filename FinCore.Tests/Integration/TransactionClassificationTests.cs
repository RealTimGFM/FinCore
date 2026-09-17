using FinCore.Application.Transactions;
using FinCore.Application.Merchants;
using FinCore.Application.Common.Persistence;
using FinCore.Application.Transactions.Classification;
using FinCore.Domain.Accounts;
using FinCore.Domain.Categories;
using FinCore.Domain.Merchants;
using FinCore.Infrastructure.Accounts;
using FinCore.Infrastructure.Categories;
using FinCore.Infrastructure.Merchants;
using FinCore.Infrastructure.Persistence;
using FinCore.Infrastructure.Transactions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Tests.Integration;

public sealed class TransactionClassificationTests
{
    [SqlServerFact]
    public async Task CreateTransaction_KnownMerchant_AutomaticallyAssignsCategory()
    {
        var options = CreateOptions("AutomaticClassification");

        try
        {
            Guid accountId;
            Guid categoryId;

            await using (var setupContext = new FinCoreDbContext(options))
            {
                await setupContext.Database.MigrateAsync();

                var account = Account.Create(
                    "TD Chequing",
                    AccountType.Chequing,
                    "CAD");
                var category = Category.Create("Restaurants");
                var memory = MerchantMemory.Create(
                    MerchantNormalizer.Normalize("McDonald's"),
                    category.Id);

                setupContext.Accounts.Add(account);
                setupContext.Categories.Add(category);
                setupContext.MerchantMemories.Add(memory);
                await setupContext.SaveChangesAsync();

                accountId = account.Id;
                categoryId = category.Id;
            }

            Guid transactionId;

            await using (var actionContext = new FinCoreDbContext(options))
            {
                var service = CreateTransactionService(actionContext);
                var result = await service.CreateAsync(
                    new CreateTransactionCommand(
                        accountId,
                        -25m,
                        "McDonald's",
                        DateTimeOffset.UtcNow));

                Assert.NotNull(result);
                Assert.Equal(categoryId, result.CategoryId);
                Assert.Equal(-25m, result.Amount);
                transactionId = result.Id;
            }

            await using var verificationContext = new FinCoreDbContext(options);
            var savedTransaction = await verificationContext.Transactions
                .AsNoTracking()
                .SingleAsync(transaction => transaction.Id == transactionId);
            var savedAccount = await verificationContext.Accounts
                .AsNoTracking()
                .SingleAsync(account => account.Id == accountId);

            Assert.Equal(categoryId, savedTransaction.CategoryId);
            Assert.Equal(-25m, savedTransaction.Amount);
            Assert.Equal(-25m, savedAccount.Balance);
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [SqlServerFact]
    public async Task CreateTransaction_UnknownMerchant_LeavesCategoryUnassigned()
    {
        var options = CreateOptions("UnclassifiedTransaction");

        try
        {
            Guid accountId;

            await using (var setupContext = new FinCoreDbContext(options))
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

            Guid transactionId;

            await using (var actionContext = new FinCoreDbContext(options))
            {
                var service = CreateTransactionService(actionContext);
                var result = await service.CreateAsync(
                    new CreateTransactionCommand(
                        accountId,
                        -25m,
                        "Cozy House",
                        DateTimeOffset.UtcNow));

                Assert.NotNull(result);
                Assert.Null(result.CategoryId);
                Assert.Equal(-25m, result.Amount);
                transactionId = result.Id;
            }

            await using var verificationContext = new FinCoreDbContext(options);
            var savedTransaction = await verificationContext.Transactions
                .AsNoTracking()
                .SingleAsync(transaction => transaction.Id == transactionId);
            var savedAccount = await verificationContext.Accounts
                .AsNoTracking()
                .SingleAsync(account => account.Id == accountId);

            Assert.Null(savedTransaction.CategoryId);
            Assert.Equal(-25m, savedTransaction.Amount);
            Assert.Equal(-25m, savedAccount.Balance);
            Assert.False(await verificationContext.MerchantMemories.AnyAsync());
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [SqlServerFact]
    public async Task ManualCategorization_TeachesFutureTransactions()
    {
        var options = CreateOptions("LearningLoop");

        try
        {
            Guid accountId;
            Guid categoryId;
            Guid firstTransactionId;

            await using (var setupContext = new FinCoreDbContext(options))
            {
                await setupContext.Database.MigrateAsync();

                var account = Account.Create(
                    "TD Chequing",
                    AccountType.Chequing,
                    "CAD");
                var category = Category.Create("Restaurants");

                setupContext.Accounts.Add(account);
                setupContext.Categories.Add(category);
                await setupContext.SaveChangesAsync();

                accountId = account.Id;
                categoryId = category.Id;
            }

            await using (var firstContext = new FinCoreDbContext(options))
            {
                var service = CreateTransactionService(firstContext);
                var first = await service.CreateAsync(
                    new CreateTransactionCommand(
                        accountId,
                        -40m,
                        "Cozy House",
                        DateTimeOffset.UtcNow));

                Assert.NotNull(first);
                Assert.Null(first.CategoryId);
                firstTransactionId = first.Id;
            }

            await using (var beforeLearningContext = new FinCoreDbContext(options))
            {
                Assert.False(await beforeLearningContext.MerchantMemories.AnyAsync());
            }

            await using (var learningContext = new FinCoreDbContext(options))
            {
                var service = CreateTransactionService(learningContext);
                var categorized = await service.SetCategoryAsync(
                    firstTransactionId,
                    categoryId);

                Assert.NotNull(categorized);
                Assert.Equal(categoryId, categorized.CategoryId);
            }

            await using (var memoryContext = new FinCoreDbContext(options))
            {
                var memory = await memoryContext.MerchantMemories
                    .AsNoTracking()
                    .SingleAsync();

                Assert.Equal("COZY HOUSE", memory.NormalizedMerchant);
                Assert.Equal(categoryId, memory.CategoryId);
            }

            Guid secondTransactionId;

            await using (var secondContext = new FinCoreDbContext(options))
            {
                var service = CreateTransactionService(secondContext);
                var second = await service.CreateAsync(
                    new CreateTransactionCommand(
                        accountId,
                        -30m,
                        "cozy-house",
                        DateTimeOffset.UtcNow));

                Assert.NotNull(second);
                Assert.Equal(categoryId, second.CategoryId);
                secondTransactionId = second.Id;
            }

            await using var verificationContext = new FinCoreDbContext(options);
            var firstSaved = await verificationContext.Transactions
                .AsNoTracking()
                .SingleAsync(transaction => transaction.Id == firstTransactionId);
            var secondSaved = await verificationContext.Transactions
                .AsNoTracking()
                .SingleAsync(transaction => transaction.Id == secondTransactionId);
            var accountSaved = await verificationContext.Accounts
                .AsNoTracking()
                .SingleAsync(account => account.Id == accountId);
            var memoryCount = await verificationContext.MerchantMemories.CountAsync();

            Assert.Equal(categoryId, firstSaved.CategoryId);
            Assert.Equal(categoryId, secondSaved.CategoryId);
            Assert.Equal(1, memoryCount);
            Assert.Equal(-70m, accountSaved.Balance);
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [SqlServerFact]
    public async Task CategorizingReversal_DoesNotTeachMerchantMemory()
    {
        var options = CreateOptions("ReversalDoesNotLearn");

        try
        {
            Guid accountId;
            Guid categoryId;

            await using (var setupContext = new FinCoreDbContext(options))
            {
                await setupContext.Database.MigrateAsync();

                var account = Account.Create(
                    "TD Chequing",
                    AccountType.Chequing,
                    "CAD");
                var category = Category.Create("Restaurants");

                setupContext.Accounts.Add(account);
                setupContext.Categories.Add(category);
                await setupContext.SaveChangesAsync();

                accountId = account.Id;
                categoryId = category.Id;
            }

            Guid reversalId;

            await using (var transactionContext = new FinCoreDbContext(options))
            {
                var service = CreateTransactionService(transactionContext);
                var original = await service.CreateAsync(
                    new CreateTransactionCommand(
                        accountId,
                        -40m,
                        "Cozy House",
                        DateTimeOffset.UtcNow));

                Assert.NotNull(original);

                var reversal = await service.ReverseAsync(
                    new ReverseTransactionCommand(
                        original.Id,
                        "Entered wrong amount",
                        DateTimeOffset.UtcNow));

                Assert.NotNull(reversal);
                Assert.True(reversal.IsReversal);
                reversalId = reversal.Id;
            }

            await using (var categorizationContext = new FinCoreDbContext(options))
            {
                var service = CreateTransactionService(categorizationContext);
                var categorized = await service.SetCategoryAsync(reversalId, categoryId);

                Assert.NotNull(categorized);
                Assert.Equal(categoryId, categorized.CategoryId);
            }

            await using (var verificationContext = new FinCoreDbContext(options))
            {
                Assert.False(await verificationContext.MerchantMemories.AnyAsync());
            }

            await using (var futureContext = new FinCoreDbContext(options))
            {
                var service = CreateTransactionService(futureContext);
                var future = await service.CreateAsync(
                    new CreateTransactionCommand(
                        accountId,
                        -10m,
                        "Entered wrong amount",
                        DateTimeOffset.UtcNow));

                Assert.NotNull(future);
                Assert.Null(future.CategoryId);
            }
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [SqlServerFact]
    public async Task KnownMerchant_ReturnsRememberedCategory()
    {
        var options = CreateOptions("KnownMerchant");

        try
        {
            Guid categoryId;

            await using (var setupContext = new FinCoreDbContext(options))
            {
                await setupContext.Database.MigrateAsync();

                var category = Category.Create("Restaurants");
                var memory = MerchantMemory.Create(
                    MerchantNormalizer.Normalize("McDonald's"),
                    category.Id);

                setupContext.Categories.Add(category);
                setupContext.MerchantMemories.Add(memory);
                await setupContext.SaveChangesAsync();

                categoryId = category.Id;
            }

            await using var actionContext = new FinCoreDbContext(options);
            var classifier = CreateClassifier(actionContext);

            var result = await classifier.ClassifyAsync("MCDONALDS");

            Assert.Equal(categoryId, result.CategoryId);
            Assert.Equal(ClassificationSource.MerchantMemory, result.Source);
            Assert.Equal(1m, result.Confidence);
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [SqlServerFact]
    public async Task UnknownMerchant_ReturnsNoMatch()
    {
        var options = CreateOptions("UnknownMerchant");

        try
        {
            await using (var setupContext = new FinCoreDbContext(options))
            {
                await setupContext.Database.MigrateAsync();
            }

            await using var actionContext = new FinCoreDbContext(options);
            var classifier = CreateClassifier(actionContext);

            var result = await classifier.ClassifyAsync("Cozy House");

            Assert.Null(result.CategoryId);
            Assert.Equal(ClassificationSource.None, result.Source);
            Assert.Equal(0m, result.Confidence);
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    [SqlServerFact]
    public async Task ArchivedRememberedCategory_ReturnsNoMatch()
    {
        var options = CreateOptions("ArchivedCategory");

        try
        {
            await using (var setupContext = new FinCoreDbContext(options))
            {
                await setupContext.Database.MigrateAsync();

                var category = Category.Create("Restaurants");
                category.Archive();

                var memory = MerchantMemory.Create(
                    MerchantNormalizer.Normalize("McDonald's"),
                    category.Id);

                setupContext.Categories.Add(category);
                setupContext.MerchantMemories.Add(memory);
                await setupContext.SaveChangesAsync();
            }

            await using var actionContext = new FinCoreDbContext(options);
            var classifier = CreateClassifier(actionContext);

            var result = await classifier.ClassifyAsync("McDonald's");

            Assert.Null(result.CategoryId);
            Assert.Equal(ClassificationSource.None, result.Source);
            Assert.Equal(0m, result.Confidence);
        }
        finally
        {
            await DeleteDatabaseAsync(options);
        }
    }

    private static TransactionCategoryClassifier CreateClassifier(
        FinCoreDbContext context)
    {
        return new TransactionCategoryClassifier(
            new MerchantMemoryRepository(context),
            new CategoryRepository(context));
    }

    private static TransactionService CreateTransactionService(
        FinCoreDbContext context)
    {
        var categoryRepository = new CategoryRepository(context);
        var merchantMemoryRepository = new MerchantMemoryRepository(context);
        var classifier = new TransactionCategoryClassifier(
            merchantMemoryRepository,
            categoryRepository);

        return new TransactionService(
            new AccountRepository(context),
            new TransactionRepository(context),
            categoryRepository,
            classifier,
            new MerchantMemoryLearner(merchantMemoryRepository),
            new EfUnitOfWork(context));
    }

    private static DbContextOptions<FinCoreDbContext> CreateOptions(string testName)
    {
        var baseConnectionString = Environment.GetEnvironmentVariable(
            "FINCORE_TEST_SQLSERVER")!;

        var connectionStringBuilder = new SqlConnectionStringBuilder(
            baseConnectionString)
        {
            InitialCatalog =
                $"FinCore_Classification_{testName}_{Guid.NewGuid():N}"
        };

        return new DbContextOptionsBuilder<FinCoreDbContext>()
            .UseSqlServer(connectionStringBuilder.ConnectionString)
            .Options;
    }

    private static async Task DeleteDatabaseAsync(
        DbContextOptions<FinCoreDbContext> options)
    {
        await using var context = new FinCoreDbContext(options);
        await context.Database.EnsureDeletedAsync();
    }
}
