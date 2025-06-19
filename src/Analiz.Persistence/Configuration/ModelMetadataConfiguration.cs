using Analiz.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analiz.Persistence.Configuration;

public class ModelMetadataConfiguration : IEntityTypeConfiguration<ModelMetadata>
{
    public void Configure(EntityTypeBuilder<ModelMetadata> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ModelName);
        builder.Property(x => x.Version);
        builder.Property(x => x.Type)
            .HasConversion<string>();

        builder.Property(x => x.Status)
            .HasConversion<string>();
        builder.Property(x => x.Configuration);
        builder.Property(x => x.TrainedAt);
        builder.Property(x => x.LastUsedAt);

        builder.Property(x => x.MetricsJson)
            .HasColumnType("jsonb");
    }
}