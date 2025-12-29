using Claims.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Claims.Infrastructure.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.DocumentId);

        builder.Property(d => d.DocumentType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(d => d.BlobUri)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.UploadedDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(d => d.OcrStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(d => d.OcrConfidence)
            .HasPrecision(5, 2);

        builder.Property(d => d.ProcessedBlobUri)
            .HasMaxLength(500);

        // Index
        builder.HasIndex(d => d.ClaimId)
            .HasDatabaseName("IX_Documents_ClaimId");
    }
}
