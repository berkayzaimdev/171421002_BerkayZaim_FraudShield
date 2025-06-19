using System.Text.Json;
using Analiz.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analiz.Persistence.Configuration;

public class FeatureConfigurationConfiguration : IEntityTypeConfiguration<FeatureConfiguration>
{
    public void Configure(EntityTypeBuilder<FeatureConfiguration> builder)
    {
        builder.ToTable("FeatureConfigurations");

        builder.HasKey(x => x.Id);

        // JSON alanları
        builder.Property(x => x.EnabledFeatures)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<Dictionary<string, bool>>(v, JsonSerializerOptions.Default))
            .IsRequired();

        builder.Property(x => x.FeatureSettings)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<Dictionary<string, FeatureSetting>>(v, JsonSerializerOptions.Default))
            .IsRequired();

        builder.Property(x => x.NormalizationParametersJson)
            .HasColumnType("jsonb")
            .IsRequired();

        // Temel alanlar
        builder.Property(x => x.Version).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        // Audit alanları için varsayılan değerler
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