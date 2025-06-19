using System.Text.Json;
using Analiz.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analiz.Persistence.Configuration;

public class RiskEvaluationConfiguration : IEntityTypeConfiguration<RiskEvaluation>
{
    public void Configure(EntityTypeBuilder<RiskEvaluation> builder)
    {
        builder.ToTable("risk_evaluations");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid");

        // Transaction bilgileri
        builder.Property(x => x.TransactionId)
            .HasColumnName("transaction_id")
            .HasColumnType("uuid")
            .IsRequired();

        // Risk değerlendirme alanları
        builder.Property(x => x.FraudProbability)
            .HasColumnName("fraud_probability")
            .HasColumnType("double precision")
            .IsRequired();

        builder.Property(x => x.AnomalyScore)
            .HasColumnName("anomaly_score")
            .HasColumnType("double precision")
            .IsRequired();

        builder.Property(x => x.RiskScore)
            .HasColumnName("risk_score")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.ConfidenceScore)
            .HasColumnName("confidence_score")
            .HasColumnType("double precision")
            .IsRequired();

        builder.Property(x => x.MLScore)
            .HasColumnName("ml_score")
            .HasColumnType("double precision")
            .IsRequired();

        builder.Property(x => x.RuleBasedScore)
            .HasColumnName("rule_based_score")
            .HasColumnType("double precision")
            .IsRequired();

        builder.Property(x => x.EnsembleWeight)
            .HasColumnName("ensemble_weight")
            .HasColumnType("double precision")
            .HasDefaultValue(1.0)
            .IsRequired();

        builder.Property(x => x.ProcessingTimeMs)
            .HasColumnName("processing_time_ms")
            .HasColumnType("bigint")
            .IsRequired(false);

        // JSON alanları
        builder.Property(x => x.RiskFactors)
            .HasColumnName("risk_factors")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<RiskFactor>>(v, JsonSerializerOptions.Default))
            .IsRequired(false);

        builder.Property(x => x.FeatureValues)
            .HasColumnName("feature_values")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<Dictionary<string, double>>(v, JsonSerializerOptions.Default))
            .IsRequired(false);

        builder.Property(x => x.FeatureImportance)
            .HasColumnName("feature_importance")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<Dictionary<string, double>>(v, JsonSerializerOptions.Default))
            .IsRequired(false);

        builder.Property(x => x.ModelMetrics)
            .HasColumnName("model_metrics")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<Dictionary<string, double>>(v, JsonSerializerOptions.Default))
            .IsRequired(false);

        builder.Property(x => x.ModelInfo)
            .HasColumnName("model_info")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, JsonSerializerOptions.Default))
            .IsRequired(false);

        builder.Property(x => x.AdditionalData)
            .HasColumnName("additional_data")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, JsonSerializerOptions.Default))
            .IsRequired(false);

        builder.Property(x => x.UsedAlgorithms)
            .HasColumnName("used_algorithms")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default))
            .IsRequired(false);

        builder.Property(x => x.Errors)
            .HasColumnName("errors")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default))
            .IsRequired(false);

        builder.Property(x => x.Warnings)
            .HasColumnName("warnings")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default))
            .IsRequired(false);

        // Metin alanları
        builder.Property(x => x.Explanation)
            .HasColumnName("explanation")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.RecommendedAction)
            .HasColumnName("recommended_action")
            .HasColumnType("text")
            .IsRequired(false);

        // Entity bilgileri
        builder.Property(x => x.EntityType)
            .HasColumnName("entity_type")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.EntityId)
            .HasColumnName("entity_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(x => x.EvaluationType)
            .HasColumnName("evaluation_type")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.RiskData)
            .HasColumnName("risk_data")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, JsonSerializerOptions.Default))
            .IsRequired(false);

        // Zaman damgaları
        builder.Property(x => x.EvaluatedAt)
            .HasColumnName("evaluated_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.EvaluationTimestamp)
            .HasColumnName("evaluation_timestamp")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(x => x.EvaluationSource)
            .HasColumnName("evaluation_source")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.ModelVersion)
            .HasColumnName("model_version")
            .HasMaxLength(50)
            .IsRequired(false);

        // Durum alanları
        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasColumnType("boolean")
            .HasDefaultValue(true)
            .IsRequired(false);

        // Audit alanları
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100)
            .HasDefaultValue("system");

        builder.Property(x => x.LastModifiedAt)
            .HasColumnName("last_modified_at")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(x => x.LastModifiedBy)
            .HasColumnName("last_modified_by")
            .HasMaxLength(100)
            .IsRequired(false);

        // Soft delete alanları
        builder.Property(x => x.IsDeleted)
            .HasColumnName("is_deleted")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired(false);

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(x => x.DeletedBy)
            .HasColumnName("deleted_by")
            .HasMaxLength(100)
            .IsRequired(false);

        // İndeksler
        builder.HasIndex(x => x.TransactionId)
            .HasDatabaseName("ix_risk_evaluations_transaction_id");

        builder.HasIndex(x => new { x.EntityType, x.EntityId })
            .HasDatabaseName("ix_risk_evaluations_entity_type_id");

        builder.HasIndex(x => x.EvaluationType)
            .HasDatabaseName("ix_risk_evaluations_type");

        builder.HasIndex(x => x.EvaluationTimestamp)
            .HasDatabaseName("ix_risk_evaluations_timestamp");

        builder.HasIndex(x => x.RiskScore)
            .HasDatabaseName("ix_risk_evaluations_risk_score");

        builder.HasIndex(x => x.FraudProbability)
            .HasDatabaseName("ix_risk_evaluations_fraud_probability");
    }
} 