using Claims.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Claims.Infrastructure.Data.Configurations;

public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.HasKey(c => c.ClaimId);

        builder.Property(c => c.PolicyId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.ClaimantId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.SubmittedDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(c => c.LastUpdatedDate)
            .IsRequired();

        builder.Property(c => c.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(c => c.FraudScore)
            .HasPrecision(5, 2);

        builder.Property(c => c.ApprovalScore)
            .HasPrecision(5, 2);

        builder.Property(c => c.AssignedSpecialistId)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(c => c.Status)
            .HasDatabaseName("IX_Claims_Status");

        builder.HasIndex(c => c.SubmittedDate)
            .HasDatabaseName("IX_Claims_SubmittedDate");

        // Relationships
        builder.HasMany(c => c.Documents)
            .WithOne(d => d.Claim)
            .HasForeignKey(d => d.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Decisions)
            .WithOne(d => d.Claim)
            .HasForeignKey(d => d.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Notifications)
            .WithOne(n => n.Claim)
            .HasForeignKey(n => n.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
