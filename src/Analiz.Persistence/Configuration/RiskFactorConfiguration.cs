using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analiz.Persistence.Configuration;

public class RiskFactorConfiguration : IEntityTypeConfiguration<RiskFactor>
{
    public void Configure(EntityTypeBuilder<RiskFactor> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.TransactionId);
        builder.Property(x => x.Code)
            .HasMaxLength(50); // ✅ Maksimum uzunluk sınırı eklendi

        builder.Property(x => x.Description)
            .HasMaxLength(250); // ✅ Açıklama için sınır eklendi

        builder.Property(x => x.Confidence)
            .HasPrecision(5, 3); // ✅ Ondalıklı sayı için daha hassas format

        // ✅ Enum alanları için string dönüşümü
        builder.Property(x => x.Severity)
            .HasConversion(
                v => v.ToString(),
                v => (RiskLevel)Enum.Parse(typeof(RiskLevel), v)
            );
        builder.Property(x => x.Type)
            .HasConversion(
                v => v.ToString(),
                v => (RiskFactorType)Enum.Parse(typeof(RiskFactorType), v)
            );

        builder
            .HasOne(x => x.AnalysisResult)
            .WithMany(ar => ar.RiskFactors)
            .HasForeignKey(x => x.AnalysisResultId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.AnalysisResult)
            .WithMany(x => x.RiskFactors)
            .HasForeignKey(x => x.AnalysisResultId)
            .IsRequired(true)  // Her RiskFactor bir AnalysisResult'a ait olmalı
            .OnDelete(DeleteBehavior.Cascade);
    }
}