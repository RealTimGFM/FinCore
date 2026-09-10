using FinCore.Domain.Accounts;
using FinCore.Domain.Transactions;
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

    public DbSet<AccountStatusChange> AccountStatusChanges =>
        Set<AccountStatusChange>();

    public DbSet<Transaction> Transactions =>
        Set<Transaction>();

    public DbSet<IdempotencyRecord> IdempotencyRecords =>
        Set<IdempotencyRecord>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FinCoreDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
