using FinCore.Domain.Accounts;
using FinCore.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinCore.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration
    : IEntityTypeConfiguration<Transaction>
{
    public void Configure(
        EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable(
            "Transactions",
            tableBuilder =>
                tableBuilder.HasCheckConstraint(
                    "CK_Transactions_Amount_NotZero",
                    "[Amount] <> 0"));

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Id)
            .ValueGeneratedNever();

        builder.Property(transaction => transaction.AccountId)
            .IsRequired();

        builder.Property(transaction => transaction.Amount)
            .HasPrecision(19, 4)
            .IsRequired();

        builder.Property(transaction => transaction.Description)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(transaction => transaction.OccurredAtUtc)
            .IsRequired();

        builder.Property(transaction => transaction.CreatedAtUtc)
            .IsRequired();

        builder.Property(transaction =>
            transaction.ReversalOfTransactionId);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(transaction => transaction.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Transaction>()
            .WithMany()
            .HasForeignKey(transaction =>
                transaction.ReversalOfTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(transaction => new
        {
            transaction.AccountId,
            transaction.OccurredAtUtc
        });

        builder.HasIndex(transaction =>
                transaction.ReversalOfTransactionId)
            .IsUnique()
            .HasFilter(
                "[ReversalOfTransactionId] IS NOT NULL");
    }
}
