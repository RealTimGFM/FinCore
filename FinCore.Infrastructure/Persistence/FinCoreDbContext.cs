using FinCore.Domain.Accounts;
using Microsoft.EntityFrameworkCore;

namespace FinCore.Infrastructure.Persistence;

public sealed class FinCoreDbContext
    : DbContext
{
    public FinCoreDbContext(
        DbContextOptions<FinCoreDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts =>
        Set<Account>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FinCoreDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}