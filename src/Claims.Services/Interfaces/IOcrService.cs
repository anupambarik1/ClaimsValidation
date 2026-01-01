using Claims.Domain.Entities;
using Claims.Services.Aws;

namespace Claims.Services.Interfaces;

public interface IOcrService
{
    Task<(string ExtractedText, decimal Confidence)> ProcessDocumentAsync(string blobUri);
    Task UpdateDocumentOcrStatusAsync(Guid documentId, string status, string? extractedText = null, decimal? confidence = null);
    //Task<DocumentIntelligenceResult> AnalyzeDocumentWithStructureAsync(string blobUri);
}
