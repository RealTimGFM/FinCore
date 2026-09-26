using FinCore.Application.Merchants;
using FinCore.Domain.Merchants;
using FinCore.Infrastructure.Merchants;
using FinCore.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Tests.Integration;

public sealed class MerchantResolutionTests
{
    [SqlServerFact]
    public async Task StarbucksVariants_ResolveToSameMerchant()
    {
        var options = CreateOptions();

        try
        {
            Guid starbucksId;

            await using (var setup = new FinCoreDbContext(options))
            {
                await setup.Database.MigrateAsync();

                var starbucks = Merchant.Create("Starbucks");
                var aliases = new[]
                {
                    MerchantAlias.Create(
                        starbucks.Id,
                        MerchantNormalizer.Normalize("STARBUCKS"),
                        MerchantAliasMatchType.Exact),
                    MerchantAlias.Create(
                        starbucks.Id,
                        MerchantNormalizer.Normalize("STARBUCKS"),
                        MerchantAliasMatchType.NumericStorePrefix),
                    MerchantAlias.Create(
                        starbucks.Id,
                        MerchantNormalizer.Normalize("STARBUCKS MOBILE"),
                        MerchantAliasMatchType.Exact),
                    MerchantAlias.Create(
                        starbucks.Id,
                        MerchantNormalizer.Normalize("STARBUCKS.COM"),
                        MerchantAliasMatchType.Exact)
                };

                setup.Merchants.Add(starbucks);
                setup.MerchantAliases.AddRange(aliases);

                await setup.SaveChangesAsync();
                starbucksId = starbucks.Id;
            }

            await using var action = new FinCoreDbContext(options);
            var catalog = new SharedMerchantCatalogRepository(action);
            var resolver = new MerchantResolver(catalog);

            var descriptions = new[]
            {
                "STARBUCKS",
                "STARBUCKS #1782",
                "STARBUCKS 04215 MONTREAL QC",
                "STARBUCKS MOBILE",
                "STARBUCKS.COM"
            };

            foreach (var description in descriptions)
            {
                var result = await resolver.ResolveAsync(description);

                Assert.NotNull(result);
                Assert.Equal(starbucksId, result.MerchantId);
                Assert.Equal("Starbucks", result.CanonicalName);
            }

            Assert.Null(await resolver.ResolveAsync("STARBUCKSIFIED 1782"));
            Assert.Null(await resolver.ResolveAsync("THE STARBUCKS STORE"));
            Assert.Null(await resolver.ResolveAsync("STARBUCKS MOBILE EXTRA"));

            var candidates = await catalog.FindCandidatesAsync(["STARBUCKS MOBILE"]);
            Assert.Equal("STARBUCKS MOBILE", Assert.Single(candidates).NormalizedAlias);
            Assert.Empty(await catalog.FindCandidatesAsync([]));
            Assert.Empty(action.ChangeTracker.Entries());
        }
        finally
        {
            await using var cleanup = new FinCoreDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    [SqlServerFact]
    public async Task Catalog_EnforcesUniqueAliasesAndRestrictsMerchantDeletion()
    {
        var options = CreateOptions();

        try
        {
            var first = Merchant.Create("First merchant");
            var second = Merchant.Create("Second merchant");

            await using (var setup = new FinCoreDbContext(options))
            {
                await setup.Database.MigrateAsync();
                setup.Merchants.AddRange(first, second);
                setup.MerchantAliases.Add(MerchantAlias.Create(
                    first.Id, "SHARED NAME", MerchantAliasMatchType.Exact));
                await setup.SaveChangesAsync();
            }

            await using (var duplicate = new FinCoreDbContext(options))
            {
                duplicate.MerchantAliases.Add(MerchantAlias.Create(
                    second.Id, "SHARED NAME", MerchantAliasMatchType.Exact));
                var error = await Assert.ThrowsAsync<DbUpdateException>(
                    () => duplicate.SaveChangesAsync());
                Assert.Equal(2601, Assert.IsType<SqlException>(error.InnerException).Number);
            }

            await using (var deletion = new FinCoreDbContext(options))
            {
                deletion.Merchants.Remove(await deletion.Merchants.SingleAsync(
                    merchant => merchant.Id == first.Id));
                var error = await Assert.ThrowsAsync<DbUpdateException>(
                    () => deletion.SaveChangesAsync());
                Assert.Equal(547, Assert.IsType<SqlException>(error.InnerException).Number);
            }

            await using var verification = new FinCoreDbContext(options);
            Assert.Equal(2, await verification.Merchants.CountAsync());
            Assert.Equal(1, await verification.MerchantAliases.CountAsync());
        }
        finally
        {
            await using var cleanup = new FinCoreDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static DbContextOptions<FinCoreDbContext> CreateOptions()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("FINCORE_TEST_SQLSERVER")!;

        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = $"FinCore_MerchantResolution_{Guid.NewGuid():N}"
        };

        return new DbContextOptionsBuilder<FinCoreDbContext>()
            .UseSqlServer(builder.ConnectionString)
            .Options;
    }
}
