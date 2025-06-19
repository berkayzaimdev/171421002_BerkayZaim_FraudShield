using System.Text.Json;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums.Rule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analiz.Persistence.Configuration;

public class FraudRuleEventConfiguration : IEntityTypeConfiguration<FraudRuleEvent>
{
    public void Configure(EntityTypeBuilder<FraudRuleEvent> builder)
    {
        builder.ToTable("fraud_rule_events");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid");

        builder.Property(x => x.RuleId)
            .HasColumnName("rule_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.RuleName)
            .HasColumnName("rule_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.RuleCode)
            .HasColumnName("rule_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.TransactionId)
            .HasColumnName("transaction_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(x => x.AccountId)
            .HasColumnName("account_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(x => x.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45)
            .IsRequired(false);

        builder.Property(x => x.DeviceInfo)
            .HasColumnName("device_info")
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(x => x.Actions)
            .HasColumnName("actions")
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize<List<RuleAction>>(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<RuleAction>>(v, (JsonSerializerOptions)null)!)
            .IsRequired();

        builder.Property(x => x.ActionDuration)
            .HasColumnName("action_duration")
            .HasColumnType("interval")
            .IsRequired(false);

        builder.Property(x => x.ActionEndDate)
            .HasColumnName("action_end_date")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(x => x.EventDetailsJson)
            .HasColumnName("event_details_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.ResolvedDate)
            .HasColumnName("resolved_date")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(x => x.ResolvedBy)
            .HasColumnName("resolved_by")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.ResolutionNotes)
            .HasColumnName("resolution_notes")
            .HasColumnType("text")
            .IsRequired(false);

        // Base entity audit / soft delete
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastModifiedAt)
            .HasColumnName("last_modified_at")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(x => x.LastModifiedBy)
            .HasColumnName("last_modified_by")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.IsDeleted)
            .HasColumnName("is_deleted")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        builder.Property(x => x.DeletedBy)
            .HasColumnName("deleted_by")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.HasIndex(x => x.RuleId)
            .HasDatabaseName("ix_fraud_rule_events_rule_id");
        builder.HasIndex(x => x.TransactionId)
            .HasDatabaseName("ix_fraud_rule_events_transaction_id");
    }
}