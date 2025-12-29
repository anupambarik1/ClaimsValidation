using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Claims.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Claims.Services.Azure;

/// <summary>
/// Azure AI Document Intelligence service for advanced document processing
/// Provides structured data extraction from invoices, receipts, and insurance documents
/// </summary>
public class AzureDocumentIntelligenceService : IOcrService
{
    private readonly DocumentAnalysisClient? _client;
    private readonly ILogger<AzureDocumentIntelligenceService> _logger;
    private readonly bool _isEnabled;
    private readonly string _modelId;

    public AzureDocumentIntelligenceService(
        IConfiguration configuration,
        ILogger<AzureDocumentIntelligenceService> logger)
    {
        _logger = logger;
        
        var endpoint = configuration["Azure:DocumentIntelligence:Endpoint"];
        var apiKey = configuration["Azure:DocumentIntelligence:ApiKey"];
        _isEnabled = configuration.GetValue<bool>("Azure:DocumentIntelligence:Enabled");
        _modelId = configuration["Azure:DocumentIntelligence:ModelId"] ?? "prebuilt-invoice";

        if (_isEnabled && !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
        {
            _client = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            _logger.LogInformation("Azure Document Intelligence initialized with model: {ModelId}", _modelId);
        }
        else
        {
            _logger.LogWarning("Azure Document Intelligence is disabled or not configured");
        }
    }

    public async Task<(string ExtractedText, decimal Confidence)> ProcessDocumentAsync(string documentPath)
    {
        if (!_isEnabled || _client == null)
        {
            _logger.LogWarning("Azure Document Intelligence not enabled, returning empty result");
            return (string.Empty, 0m);
        }

        try
        {
            _logger.LogInformation("Processing document with Azure Document Intelligence: {Path}", documentPath);

            AnalyzeDocumentOperation operation;

            // Check if it's a URL or local file
            if (Uri.TryCreate(documentPath, UriKind.Absolute, out var uri) && 
                (uri.Scheme == "http" || uri.Scheme == "https"))
            {
                operation = await _client.AnalyzeDocumentFromUriAsync(
                    WaitUntil.Completed, 
                    _modelId, 
                    uri);
            }
            else
            {
                // Local file
                using var stream = File.OpenRead(documentPath);
                operation = await _client.AnalyzeDocumentAsync(
                    WaitUntil.Completed, 
                    _modelId, 
                    stream);
            }

            var result = operation.Value;

            // Extract text content
            var extractedText = string.Join("\n", result.Pages
                .SelectMany(p => p.Lines)
                .Select(l => l.Content));

            // Calculate average confidence
            var confidence = result.Pages.Any()
                ? (decimal)result.Pages.Average(p => p.Words.Any() 
                    ? p.Words.Average(w => w.Confidence) 
                    : 0.0)
                : 0m;

            _logger.LogInformation("Document processed successfully. Confidence: {Confidence:P2}", confidence);

            return (extractedText, confidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document with Azure Document Intelligence");
            return (string.Empty, 0m);
        }
    }

    /// <summary>
    /// Update document OCR status in database
    /// Note: This is a pass-through - actual DB update should be done by ClaimsService
    /// </summary>
    public Task UpdateDocumentOcrStatusAsync(
        Guid documentId, 
        string status, 
        string? extractedText = null, 
        decimal? confidence = null)
    {
        _logger.LogInformation(
            "OCR status update for document {DocumentId}: {Status}, Confidence: {Confidence:P2}", 
            documentId, status, confidence);
        
        // Note: The actual database update is handled by ClaimsService
        // This method is here to satisfy the interface contract
        return Task.CompletedTask;
    }

    /// <summary>
    /// Extract structured invoice data using Azure Document Intelligence
    /// </summary>
    public async Task<InvoiceExtractionResult> ExtractInvoiceDataAsync(string documentPath)
    {
        var result = new InvoiceExtractionResult();

        if (!_isEnabled || _client == null)
        {
            _logger.LogWarning("Azure Document Intelligence not enabled");
            return result;
        }

        try
        {
            AnalyzeDocumentOperation operation;

            if (Uri.TryCreate(documentPath, UriKind.Absolute, out var uri) && 
                (uri.Scheme == "http" || uri.Scheme == "https"))
            {
                operation = await _client.AnalyzeDocumentFromUriAsync(
                    WaitUntil.Completed, 
                    "prebuilt-invoice", 
                    uri);
            }
            else
            {
                using var stream = File.OpenRead(documentPath);
                operation = await _client.AnalyzeDocumentAsync(
                    WaitUntil.Completed, 
                    "prebuilt-invoice", 
                    stream);
            }

            var document = operation.Value.Documents.FirstOrDefault();
            if (document == null) return result;

            // Extract invoice fields
            if (document.Fields.TryGetValue("InvoiceId", out var invoiceId))
                result.InvoiceNumber = invoiceId.Content;

            if (document.Fields.TryGetValue("InvoiceDate", out var invoiceDate) && 
                invoiceDate.FieldType == DocumentFieldType.Date)
                result.InvoiceDate = invoiceDate.Value.AsDate();

            if (document.Fields.TryGetValue("InvoiceTotal", out var total) && 
                total.FieldType == DocumentFieldType.Currency)
                result.TotalAmount = (decimal)total.Value.AsCurrency().Amount;

            if (document.Fields.TryGetValue("VendorName", out var vendor))
                result.VendorName = vendor.Content;

            if (document.Fields.TryGetValue("CustomerName", out var customer))
                result.CustomerName = customer.Content;

            result.Confidence = (decimal)document.Confidence;
            result.Success = true;

            _logger.LogInformation("Invoice extracted: {InvoiceNumber}, Total: {Total:C}", 
                result.InvoiceNumber, result.TotalAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting invoice data");
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}

/// <summary>
/// Result of invoice data extraction
/// </summary>
public class InvoiceExtractionResult
{
    public bool Success { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTimeOffset? InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? VendorName { get; set; }
    public string? CustomerName { get; set; }
    public decimal Confidence { get; set; }
    public string? ErrorMessage { get; set; }
    public List<InvoiceLineItem> LineItems { get; set; } = new();
}

public class InvoiceLineItem
{
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}
