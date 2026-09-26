using FinCore.Domain.Merchants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinCore.Infrastructure.Persistence.Configurations;

public sealed class MerchantAliasConfiguration
    : IEntityTypeConfiguration<MerchantAlias>
{
    public void Configure(EntityTypeBuilder<MerchantAlias> builder)
    {
        builder.ToTable("MerchantAliases");

        builder.HasKey(alias => alias.Id);

        builder.Property(alias => alias.Id)
            .ValueGeneratedNever();

        builder.Property(alias => alias.NormalizedAlias)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(alias => alias.MatchType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasOne<Merchant>()
            .WithMany()
            .HasForeignKey(alias => alias.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(alias => new
        {
            alias.MatchType,
            alias.NormalizedAlias
        })
        .IsUnique();

        builder.HasIndex(alias => alias.MerchantId);
    }
}
