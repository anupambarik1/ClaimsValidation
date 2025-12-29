using Claims.Domain.Entities;

namespace Claims.Services.Interfaces;

public interface IDocumentAnalysisService
{
    Task<string> ClassifyDocumentAsync(string extractedText);
    Task<bool> ValidateDocumentContentAsync(Document document);
}
