using FinCore.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinCore.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration
    : IEntityTypeConfiguration<Account>
{
    public void Configure(
        EntityTypeBuilder<Account> builder)
    {
        builder.ToTable(
            "Accounts",
            tableBuilder => tableBuilder.HasCheckConstraint(
                "CK_Accounts_Status_ClosedAtUtc",
                "([Status] = 'Active' AND [ClosedAtUtc] IS NULL) OR " +
                "([Status] = 'Closed' AND [ClosedAtUtc] IS NOT NULL)"));

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(account => account.Type)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(account => account.Currency)
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(account => account.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AccountStatus.Active)
            .HasSentinel((AccountStatus)0)
            .IsRequired();

        builder.Property(account => account.Balance)
            .HasPrecision(19, 4)
            .IsRequired();

        builder.Property(account => account.BalanceAsOfUtc)
            .IsRequired();

        builder.Property(account => account.CreatedAtUtc)
            .IsRequired();

        builder.Property(account => account.ClosedAtUtc);

        builder.HasMany(account => account.StatusChanges)
            .WithOne()
            .HasForeignKey(statusChange => statusChange.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(account => account.Currency);

        builder.HasIndex(account => account.Status);
    }
}
