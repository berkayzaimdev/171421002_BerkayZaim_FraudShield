using System.Text.Json;
using Analiz.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analiz.Persistence.Configuration;

public class FraudAlertConfiguration : IEntityTypeConfiguration<FraudAlert>
{
    public void Configure(EntityTypeBuilder<FraudAlert> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransactionId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<string>();

        builder.Property(x => x.Status)
            .HasConversion<string>();

        builder.OwnsOne(x => x.RiskScore, rs =>
        {
            rs.Property(r => r.Score).HasColumnName("RiskScore");
            rs.Property(r => r.Level)
                .HasColumnName("RiskLevel")
                .HasConversion<string>();
        });

        builder.Property(x => x.Factors)
            .HasColumnName("Factors")
            .HasConversion(
                v => string.Join(",", v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            );

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ResolvedAt).IsRequired(false);
        builder.Property(x => x.Resolution).HasMaxLength(500).IsRequired(false);
        builder.Property(x => x.CreatedBy).IsRequired(false);
        builder.Property(x => x.LastModifiedBy).IsRequired(false);
        builder.Property(x => x.DeletedBy).IsRequired(false);

        
        builder.HasOne(x => x.AnalysisResult)
            .WithOne(x => x.FraudAlert)
            .HasForeignKey<FraudAlert>(x => x.AnalysisResultId)
            .IsRequired(true);  // Her FraudAlert mutlaka bir AnalysisResult'a ait olmalı
        
    }
}