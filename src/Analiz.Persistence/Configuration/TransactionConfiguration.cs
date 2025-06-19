using System.Text.Json;
using Analiz.Domain.Entities;
using Analiz.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analiz.Persistence.Configuration;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.MerchantId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.TransactionTime)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.FlagsJson)
            .HasColumnType("jsonb");

        builder.Property(x => x.Details)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<TransactionDetails>(v, JsonSerializerOptions.Default));

        builder.OwnsOne(x => x.RiskScore, rs =>
        {
            rs.Property(r => r.Score).HasColumnName("RiskScore");
            rs.Property(r => r.Level)
                .HasColumnName("RiskLevel")
                .HasConversion<string>();
        });

        builder.Property(x => x.DeviceInfo)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<DeviceInfo>(v, JsonSerializerOptions.Default));

        builder.Property(x => x.Location)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<Location>(v, JsonSerializerOptions.Default));
        builder.Ignore(x => x.Flags);
    }
}