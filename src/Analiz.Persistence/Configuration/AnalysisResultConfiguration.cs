using System.Text.Json;
using Analiz.Domain.Entities;
using Analiz.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analiz.Persistence.Configuration;

public class AnalysisResultConfiguration : IEntityTypeConfiguration<AnalysisResult>
{
    public void Configure(EntityTypeBuilder<AnalysisResult> builder)
    {
        // Primary key
        builder.HasKey(x => x.Id);

        // Basic properties
        builder.Property(x => x.TransactionId);
        builder.Property(x => x.AnomalyScore);
        builder.Property(x => x.FraudProbability);
        builder.Property(x => x.AnalyzedAt);
        builder.Property(x => x.Error).HasMaxLength(500);
        builder.Property(x => x.TotalRuleCount).HasDefaultValue(0);
        builder.Property(x => x.TriggeredRuleCount).HasDefaultValue(0);

        // RiskScore configuration (existing)
        builder.OwnsOne(x => x.RiskScore, rs =>
        {
            rs.Property(r => r.Score).HasColumnName("RiskScore");
            rs.Property(r => r.Level)
                .HasColumnName("RiskLevel")
                .HasConversion<string>();
            rs.Property(r => r.Factors)
                .HasColumnName("RiskFactors")
                .HasConversion(
                    v => string.Join(",", v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );
            rs.Property(r => r.CalculatedAt)
                .HasColumnName("RiskScore_CalculatedAt");
        });

        // Enums to string conversion
        builder.Property(x => x.Decision).HasConversion<string>();
        builder.Property(x => x.Status).HasConversion<string>();

        // JSON fields
        builder.Property(x => x.TriggeredRules)
            .HasColumnName("TriggeredRulesJson")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, GetJsonOptions()),
                v => JsonSerializer.Deserialize<List<TriggeredRuleInfo>>(v, GetJsonOptions()) 
                     ?? new List<TriggeredRuleInfo>()
            );

        builder.Property(x => x.AppliedActions)
            .HasColumnName("AppliedActions")
            .HasConversion(
                v => string.Join(",", v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            );

        // MLAnalysis is marked as [NotMapped] since database columns don't exist

        // Relationships
        builder.HasMany(x => x.RiskFactors)
            .WithOne(x => x.AnalysisResult)
            .HasForeignKey(x => x.AnalysisResultId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.FraudAlert)
            .WithOne(x => x.AnalysisResult)
            .HasForeignKey<FraudAlert>(x => x.AnalysisResultId);

        // Indexes
        builder.HasIndex(x => x.TransactionId).HasDatabaseName("IX_AnalysisResults_TransactionId");
        builder.HasIndex(x => x.AnalyzedAt).HasDatabaseName("IX_AnalysisResults_AnalyzedAt");
    }

    /// <summary>
    /// JSON serialization options
    /// </summary>
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }
}