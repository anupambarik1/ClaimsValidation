using Claims.Domain.Enums;

namespace Claims.Domain.Entities;

public class Document
{
    public Guid DocumentId { get; set; }
    public Guid ClaimId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string BlobUri { get; set; } = string.Empty;
    public DateTime UploadedDate { get; set; }
    public OcrStatus OcrStatus { get; set; }
    public decimal? OcrConfidence { get; set; }
    public string? ProcessedBlobUri { get; set; }
    public string? ExtractedText { get; set; }
    
    // Navigation property
    public virtual Claim Claim { get; set; } = null!;
}
