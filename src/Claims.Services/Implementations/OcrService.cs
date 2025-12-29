using Claims.Domain.Enums;
using Claims.Infrastructure.Data;
using Claims.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Tesseract;
using Microsoft.Extensions.Configuration;

namespace Claims.Services.Implementations;

public class OcrService : IOcrService
{
    private readonly ClaimsDbContext _context;
    private readonly string _tessdataPath;

    public OcrService(ClaimsDbContext context, IConfiguration configuration)
    {
        _context = context;
        _tessdataPath = configuration["TesseractSettings:TessdataPath"] ?? "./tessdata";
    }

    public async Task<(string ExtractedText, decimal Confidence)> ProcessDocumentAsync(string blobUri)
    {
        try
        {
            // For POC: blobUri is treated as a local file path
            // In production, this would download from Azure Blob Storage first
            
            if (!File.Exists(blobUri))
            {
                throw new FileNotFoundException($"Document not found at path: {blobUri}");
            }

            // Run OCR in a background task to avoid blocking
            return await Task.Run(() =>
            {
                using var engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default);
                using var img = Pix.LoadFromFile(blobUri);
                using var page = engine.Process(img);
                
                var text = page.GetText();
                var confidence = page.GetMeanConfidence();

                return (text, (decimal)confidence);
            });
        }
        catch (Exception ex)
        {
            // Log error and return empty result with low confidence
            Console.WriteLine($"OCR Error: {ex.Message}");
            return (string.Empty, 0.0m);
        }
    }

    public async Task UpdateDocumentOcrStatusAsync(
        Guid documentId,
        string status,
        string? extractedText = null,
        decimal? confidence = null)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document != null && Enum.TryParse<OcrStatus>(status, out var ocrStatus))
        {
            document.OcrStatus = ocrStatus;
            document.ExtractedText = extractedText;
            document.OcrConfidence = confidence;
            await _context.SaveChangesAsync();
        }
    }
}
