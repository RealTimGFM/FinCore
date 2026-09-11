using FinCore.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinCore.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration
    : IEntityTypeConfiguration<Category>
{
    public void Configure(
        EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Id)
            .ValueGeneratedNever();

        builder.Property(category => category.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(category => category.IsArchived)
            .IsRequired();

        builder.Property(category => category.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(category => category.Name);
    }
}
