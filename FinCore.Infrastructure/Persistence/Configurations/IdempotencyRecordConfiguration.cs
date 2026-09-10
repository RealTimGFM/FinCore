using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinCore.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration
    : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(
        EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.Id)
            .ValueGeneratedNever();

        builder.Property(record => record.Key)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(record => record.RequestHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(record => record.StatusCode)
            .IsRequired();

        builder.Property(record => record.ResponseBody)
            .HasColumnType("nvarchar(max)");

        builder.Property(record => record.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(record => record.Key)
            .IsUnique();
    }
}
