using Claims.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Claims.Infrastructure.Data.Configurations;

public class DecisionConfiguration : IEntityTypeConfiguration<Decision>
{
    public void Configure(EntityTypeBuilder<Decision> builder)
    {
        builder.HasKey(d => d.DecisionId);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(d => d.DecisionDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(d => d.DecidedBy)
            .HasMaxLength(100);

        builder.Property(d => d.ReviewerId)
            .HasMaxLength(100);

        builder.Property(d => d.FraudScore)
            .HasPrecision(5, 2);

        builder.Property(d => d.ApprovalScore)
            .HasPrecision(5, 2);

        // Index
        builder.HasIndex(d => d.ClaimId)
            .HasDatabaseName("IX_Decisions_ClaimId");
    }
}
