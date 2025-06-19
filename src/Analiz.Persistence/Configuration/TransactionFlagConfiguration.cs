using Analiz.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Analiz.Persistence.Configuration;

public class TransactionFlagConfiguration : IEntityTypeConfiguration<TransactionFlag>
{
    public void Configure(EntityTypeBuilder<TransactionFlag> builder)
    {
        builder.HasNoKey();
        builder.Property(x => x.Code);
        builder.Property(x => x.Description);
        builder.Property(x => x.Severity)
            .HasConversion<string>();
        builder.Property(x => x.CreatedAt);
    }
}