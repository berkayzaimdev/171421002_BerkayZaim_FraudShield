using System.Text.Json;
using Analiz.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analiz.Persistence.Configuration;

public class FeatureImportanceConfiguration : IEntityTypeConfiguration<FeatureImportance>
{
    public void Configure(EntityTypeBuilder<FeatureImportance> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ModelName);
        builder.Property(x => x.FeatureName);
        builder.Property(x => x.Importance);
        builder.Property(x => x.Category)
            .HasConversion<string>();
        builder.Property(x => x.CalculatedAt);

        builder.Property(x => x.Statistics)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<Dictionary<string, double>>(v, JsonSerializerOptions.Default));
        builder.Property(x => x.CreatedBy)
            .HasMaxLength(50)
            .HasDefaultValue("system")
            .IsRequired();

        builder.Property(x => x.LastModifiedBy)
            .HasMaxLength(50)
            .HasDefaultValue("system")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.LastModifiedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        // Soft delete
        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.DeletedBy)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.DeletedAt)
            .IsRequired(false);
    }
}