using FinCore.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinCore.Infrastructure.Persistence.Configurations;

public sealed class AccountStatusChangeConfiguration
    : IEntityTypeConfiguration<AccountStatusChange>
{
    public void Configure(
        EntityTypeBuilder<AccountStatusChange> builder)
    {
        builder.ToTable("AccountStatusChanges");

        builder.HasKey(statusChange => statusChange.Id);

        builder.Property(statusChange => statusChange.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(statusChange => statusChange.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(statusChange => statusChange.Reason)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(statusChange => statusChange.Source)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(statusChange => statusChange.ChangedAtUtc)
            .IsRequired();

        builder.HasIndex(statusChange => new
        {
            statusChange.AccountId,
            statusChange.ChangedAtUtc
        });
    }
}
