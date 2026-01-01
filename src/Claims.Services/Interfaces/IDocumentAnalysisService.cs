using Claims.Domain.Entities;
using Claims.Services.Aws;

namespace Claims.Services.Interfaces;

public interface IDocumentAnalysisService
{
    Task<string> ClassifyDocumentAsync(string extractedText);
    Task<bool> ValidateDocumentContentAsync(Document document);

    Task<DocumentIntelligenceResult> AnalyzeDocumentWithStructureAsync(string blobUri);
}
