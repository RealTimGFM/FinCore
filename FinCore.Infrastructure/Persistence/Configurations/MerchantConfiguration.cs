using FinCore.Domain.Merchants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinCore.Infrastructure.Persistence.Configurations;

public sealed class MerchantConfiguration
    : IEntityTypeConfiguration<Merchant>
{
    public void Configure(EntityTypeBuilder<Merchant> builder)
    {
        builder.ToTable("Merchants");

        builder.HasKey(merchant => merchant.Id);

        builder.Property(merchant => merchant.Id)
            .ValueGeneratedNever();

        builder.Property(merchant => merchant.CanonicalName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(merchant => merchant.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(merchant => merchant.CanonicalName);
    }
}
