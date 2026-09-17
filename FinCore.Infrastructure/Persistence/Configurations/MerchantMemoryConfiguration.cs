using FinCore.Domain.Categories;
using FinCore.Domain.Merchants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinCore.Infrastructure.Persistence.Configurations;

public sealed class MerchantMemoryConfiguration
    : IEntityTypeConfiguration<MerchantMemory>
{
    public void Configure(EntityTypeBuilder<MerchantMemory> builder)
    {
        builder.ToTable("MerchantMemories");

        builder.HasKey(memory => memory.Id);

        builder.Property(memory => memory.Id)
            .ValueGeneratedNever();

        builder.Property(memory => memory.NormalizedMerchant)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(memory => memory.CategoryId)
            .IsRequired();

        builder.Property(memory => memory.CreatedAtUtc)
            .IsRequired();

        builder.Property(memory => memory.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(memory => memory.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(memory => memory.NormalizedMerchant)
            .IsUnique();

        builder.HasIndex(memory => memory.CategoryId);
    }
}
