using System.Text.Json;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using FraudShield.TransactionAnalysis.Domain.Enums.Rule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analiz.Persistence.Configuration;

public class FraudRuleConfiguration : IEntityTypeConfiguration<FraudRule>
{
    public void Configure(EntityTypeBuilder<FraudRule> builder)
    {
        builder.ToTable("fraud_rules");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id")
            .HasColumnType("uuid");

        builder.Property(r => r.RuleCode)
            .HasColumnName("rule_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(r => r.Category)
            .HasColumnName("category")
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(r => r.Type)
            .HasColumnName("type")
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(r => r.ImpactLevel)
            .HasColumnName("impact_level")
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();

        // JSONB dönüşümü, JsonSerializerOptions'ı açıkça null gösteriyoruz
        builder.Property(r => r.Actions)
            .HasColumnName("actions")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize<List<RuleAction>>(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<RuleAction>>(v, (JsonSerializerOptions)null)!)
            .IsRequired();

        builder.Property(r => r.ActionDuration)
            .HasColumnName("action_duration")
            .HasColumnType("interval")
            .IsRequired(false);

        builder.Property(r => r.ValidFrom)
            .HasColumnName("valid_from")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(r => r.ValidTo)
            .HasColumnName("valid_to")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(r => r.Priority)
            .HasColumnName("priority")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(r => r.Condition)
            .HasColumnName("condition")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(r => r.ConfigurationJson)
            .HasColumnName("configuration_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(r => r.LastModified)
            .HasColumnName("last_modified")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(r => r.ModifiedBy)
            .HasColumnName("modified_by")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(r => r.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(r => r.LastModifiedAt)
            .HasColumnName("last_modified_at")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(r => r.LastModifiedBy)
            .HasColumnName("last_modified_by")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(r => r.IsDeleted)
            .HasColumnName("is_deleted")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(r => r.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(r => r.DeletedBy)
            .HasColumnName("deleted_by")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.HasIndex(r => r.RuleCode)
            .IsUnique()
            .HasDatabaseName("ix_fraud_rules_rule_code");
    }
}