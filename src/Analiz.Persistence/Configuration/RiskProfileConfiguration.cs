using System.Text.Json;
using Analiz.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analiz.Persistence.Configuration;

public class RiskProfileConfiguration : IEntityTypeConfiguration<RiskProfile>
{
    public void Configure(EntityTypeBuilder<RiskProfile> builder)
    {
        builder.HasKey(x => x.UserId);

        builder.Property(x => x.AverageRiskScore)
            .IsRequired();

        builder.Property(x => x.TransactionCount)
            .IsRequired();

        builder.Property(x => x.HighRiskTransactionCount)
            .IsRequired();

        builder.Property(x => x.AverageTransactionAmount)
            .IsRequired();

        builder.Property(x => x.LastUpdated)
            .IsRequired();

        builder.Property(x => x.CommonRiskFactors)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<Dictionary<string, int>>(v, JsonSerializerOptions.Default)
            );
    }
}