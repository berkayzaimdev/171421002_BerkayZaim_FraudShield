using Analiz.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analiz.Persistence.Configuration;

public class BlacklistItemConfiguration : IEntityTypeConfiguration<BlacklistItem>
{
    public void Configure(EntityTypeBuilder<BlacklistItem> builder)
    {
        builder.ToTable("blacklist_items");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid");

        // Type (enum → smallint)
        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();

        // Value
        builder.Property(x => x.Value)
            .HasColumnName("value")
            .HasMaxLength(200)
            .IsRequired();

        // Reason
        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasColumnType("text")
            .IsRequired();

        // Optional foreign keys
        builder.Property(x => x.RuleId)
            .HasColumnName("rule_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(x => x.EventId)
            .HasColumnName("event_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        // Expiry
        builder.Property(x => x.ExpiryDate)
            .HasColumnName("expiry_date")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        // Status (enum → smallint)
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired();

        // Audit: who added
        builder.Property(x => x.AddedBy)
            .HasColumnName("added_by")
            .HasMaxLength(100)
            .IsRequired();

        // Audit: who invalidated
        builder.Property(x => x.InvalidatedBy)
            .HasColumnName("invalidated_by")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.InvalidatedAt)
            .HasColumnName("invalidated_at")
            .HasColumnType("timestamptz")
            .IsRequired(false);

        // Base Entity audit fields
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

        // Soft Delete
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

        // Index on Value for quick lookup
        builder.HasIndex(x => x.Value)
            .HasDatabaseName("ix_blacklist_items_value");
    }
}