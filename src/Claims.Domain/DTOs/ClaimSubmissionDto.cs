using Claims.Domain.Enums;

namespace Claims.Domain.DTOs;

public class ClaimSubmissionDto
{
    public string PolicyId { get; set; } = string.Empty;
    public string ClaimantId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<DocumentUploadDto> Documents { get; set; } = new();
}

public class DocumentUploadDto
{
    public DocumentType DocumentType { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Base64Content { get; set; } = string.Empty; // Set by controller after reading file
}
