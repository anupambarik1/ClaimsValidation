using Claims.Domain.Entities;
using Claims.Domain.Enums;
using Claims.Infrastructure.Data;
using Claims.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Amazon.Textract;
using Amazon.Textract.Model;
using Amazon;
using Amazon.Runtime;
using System.Text;
using System.Text.RegularExpressions;

namespace Claims.Services.Aws;

/// <summary>
/// AWS Textract Document Intelligence Service
/// Provides advanced document analysis including:
/// - Text extraction with confidence scores
/// - Table extraction and parsing
/// - Form field detection and value extraction
/// - Document type classification
/// - Invoice/receipt specific parsing
/// </summary>
public class AWSTextractDocumentIntelligenceService : IDocumentAnalysisService
{
    private readonly ILogger<AWSTextractDocumentIntelligenceService> _logger;
    private readonly ClaimsDbContext _context;
    private readonly IAmazonTextract _textractClient;
    private readonly bool _isEnabled;
    private readonly string _s3BucketName;

    public AWSTextractDocumentIntelligenceService(
        IConfiguration configuration,
        ILogger<AWSTextractDocumentIntelligenceService> logger,
        ClaimsDbContext context)
    {
        _logger = logger;
        _context = context;
        _isEnabled = configuration.GetValue<bool>("AWS:Enabled", false);
        _s3BucketName = configuration["AWS:S3BucketName"] ?? configuration["AWS:S3Bucket"] ?? "claims-documents";
        
        var accessKey = configuration["AWS:AccessKey"];
        var secretKey = configuration["AWS:SecretKey"];
        var regionName = configuration["AWS:Region"] ?? "us-east-1";
        
        AWSCredentials credentials = new BasicAWSCredentials(accessKey, secretKey);
        var region = RegionEndpoint.GetBySystemName(regionName);
        _textractClient = new AmazonTextractClient(credentials, region);
    }
    ///this method is working copy of AWS Textract OCR functionality -- Anupam Barik----- 01-Jan-2026
    /// <summary>
    /// Analyzes document and extracts structured data using Textract
    /// Supports TABLES and FORMS feature types for rich data extraction
    /// </summary>
    public async Task<DocumentIntelligenceResult> AnalyzeDocumentWithStructureAsync(string blobUri)
    {
        if (!_isEnabled)
        {
            _logger.LogWarning("AWS Textract service is disabled");
            return new DocumentIntelligenceResult { Success = false };
        }

        try
        {
            var (bucket, key) = ParseS3Uri(blobUri);
            if (bucket == null || key == null)
            {
                _logger.LogWarning("Invalid S3 URI: {Uri}", blobUri);
                return new DocumentIntelligenceResult { Success = false, Error = "Invalid S3 URI format" };
            }

            _logger.LogInformation("Analyzing document from S3: {Bucket}/{Key}", bucket, key);

            // Use AnalyzeDocument for TABLES and FORMS
            var analyzeRequest = new AnalyzeDocumentRequest
            {
                FeatureTypes = new List<string> { "TABLES", "FORMS" },
                Document = new Amazon.Textract.Model.Document
                {
                    S3Object = new Amazon.Textract.Model.S3Object { Bucket = bucket, Name = key }
                }
            };

            var analyzeResponse = await _textractClient.AnalyzeDocumentAsync(analyzeRequest);

            var result = new DocumentIntelligenceResult { Success = true };

            // Extract text blocks
            result.ExtractedText = ExtractTextFromBlocks(analyzeResponse.Blocks, out var textConfidence);
            result.TextConfidence = textConfidence;

            // Extract tables
            result.Tables = ExtractTablesFromBlocks(analyzeResponse.Blocks);

            // Extract form fields
            result.FormFields = ExtractFormFieldsFromBlocks(analyzeResponse.Blocks);

            // Detect document type
            result.DocumentType = ClassifyDocumentType(result.ExtractedText);

            _logger.LogInformation(
                "Document analysis complete - Type: {Type}, Confidence: {Confidence}, Tables: {TableCount}, Forms: {FormCount}",
                result.DocumentType, result.TextConfidence, result.Tables.Count, result.FormFields.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing document with AWS Textract");
            return new DocumentIntelligenceResult
            {
                Success = false,
                Error = $"Textract analysis failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Classifies document type based on extracted content
    /// </summary>
    public async Task<string> ClassifyDocumentAsync(string extractedText)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
            return "Unknown";

        await Task.Delay(10); // Simulate processing

        return ClassifyDocumentType(extractedText);
    }

    /// <summary>
    /// Validates document content and OCR quality
    /// </summary>
    public async Task<bool> ValidateDocumentContentAsync(Claims.Domain.Entities.Document document)
    {
        if (string.IsNullOrWhiteSpace(document.ExtractedText))
            return false;

        // Check confidence threshold
        if (document.OcrConfidence.HasValue && document.OcrConfidence < 0.5m)
        {
            _logger.LogWarning(
                "Document {DocumentId} failed confidence check: {Confidence}",
                document.DocumentId, document.OcrConfidence);
            return false;
        }

        await Task.Delay(10); // Simulate processing

        // Check for suspicious patterns
        var textLower = document.ExtractedText.ToLowerInvariant();
        var suspiciousPatterns = new[]
        {
            "photoshop", "edited", "modified", "lorem ipsum", "sample", "test document", "draft"
        };

        if (suspiciousPatterns.Any(p => textLower.Contains(p)))
        {
            _logger.LogWarning("Document {DocumentId} contains suspicious patterns", document.DocumentId);
            return false;
        }

        // Check minimum text length
        if (document.ExtractedText.Length < 20)
        {
            _logger.LogWarning("Document {DocumentId} has insufficient content", document.DocumentId);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Extracts text from Textract blocks
    /// </summary>
    private string ExtractTextFromBlocks(List<Block> blocks, out decimal confidence)
    {
        var sb = new StringBuilder();
        var confidences = new List<decimal>();

        foreach (var block in blocks)
        {
            if (block.BlockType?.ToString() == "LINE" && !string.IsNullOrEmpty(block.Text))
            {
                sb.AppendLine(block.Text);
                if (block.Confidence > 0)
                {
                    confidences.Add((decimal)block.Confidence);
                }
            }
        }

        confidence = confidences.Any() ? (decimal)confidences.Average() : 0m;
        return sb.ToString();
    }

    /// <summary>
    /// Extracts tables from Textract blocks
    /// </summary>
    private List<DocumentTable> ExtractTablesFromBlocks(List<Block> blocks)
    {
        var tables = new List<DocumentTable>();
        var tableBlocks = blocks.Where(b => b.BlockType?.ToString() == "TABLE").ToList();

        foreach (var tableBlock in tableBlocks)
        {
            var table = new DocumentTable
            {
                TableId = tableBlock.Id,
                Confidence = (decimal?)tableBlock.Confidence ?? 0m,
                Rows = new List<List<string>>()
            };

            // Extract rows from table
            var rowIds = tableBlock.Relationships
                ?.Where(r => r.Type == "CHILD")
                .FirstOrDefault()
                ?.Ids ?? new List<string>();

            var currentRow = new List<string>();
            foreach (var rowId in rowIds)
            {
                var cell = blocks.FirstOrDefault(b => b.Id == rowId);
                if (cell != null)
                {
                    var cellText = ExtractCellContent(cell, blocks);
                    currentRow.Add(cellText);

                    // Check if this is end of row (based on Textract structure)
                    if (currentRow.Count >= 5 || rowId == rowIds.Last())
                    {
                        if (currentRow.Any(c => !string.IsNullOrEmpty(c)))
                        {
                            table.Rows.Add(new List<string>(currentRow));
                        }
                        if (rowId != rowIds.Last())
                        {
                            currentRow.Clear();
                        }
                    }
                }
            }

            if (table.Rows.Any())
            {
                tables.Add(table);
            }
        }

        return tables;
    }

    /// <summary>
    /// Extracts form fields from Textract blocks
    /// </summary>
    private Dictionary<string, FormField> ExtractFormFieldsFromBlocks(List<Block> blocks)
    {
        var formFields = new Dictionary<string, FormField>(StringComparer.OrdinalIgnoreCase);
        var keyValueSets = blocks.Where(b => b.BlockType?.ToString() == "KEY_VALUE_SET").ToList();

        foreach (var kvSet in keyValueSets)
        {
            // Skip values - we'll process them as part of key processing
            if (kvSet.EntityTypes?.Any(e => e?.ToString() == "VALUE") == true)
                continue;

            var keyName = ExtractTextFromBlock(kvSet, blocks);
            var valueIds = kvSet.Relationships
                ?.Where(r => r.Type == "VALUE")
                .FirstOrDefault()
                ?.Ids ?? new List<string>();

            var valueText = string.Empty;
            var valueConfidence = 0m;

            if (valueIds.Any())
            {
                var valueBlock = blocks.FirstOrDefault(b => b.Id == valueIds.First());
                if (valueBlock != null)
                {
                    valueText = ExtractTextFromBlock(valueBlock, blocks);
                    valueConfidence = (decimal?)valueBlock.Confidence ?? 0m;
                }
            }

            if (!string.IsNullOrEmpty(keyName))
            {
                formFields[keyName] = new FormField
                {
                    Key = keyName,
                    Value = valueText,
                    Confidence = valueConfidence
                };
            }
        }

        return formFields;
    }

    /// <summary>
    /// Extracts text content from a specific block and its children
    /// </summary>
    private string ExtractTextFromBlock(Block block, List<Block> allBlocks)
    {
        if (!string.IsNullOrEmpty(block.Text))
            return block.Text;

        var childIds = block.Relationships
            ?.Where(r => r.Type == "CHILD")
            .FirstOrDefault()
            ?.Ids ?? new List<string>();

        var sb = new StringBuilder();
        foreach (var childId in childIds)
        {
            var childBlock = allBlocks.FirstOrDefault(b => b.Id == childId);
            if (childBlock?.Text != null)
            {
                sb.Append(childBlock.Text).Append(" ");
            }
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Extracts content from a table cell
    /// </summary>
    private string ExtractCellContent(Block cellBlock, List<Block> allBlocks)
    {
        var childIds = cellBlock.Relationships
            ?.Where(r => r.Type == "CHILD")
            .FirstOrDefault()
            ?.Ids ?? new List<string>();

        var sb = new StringBuilder();
        foreach (var childId in childIds)
        {
            var childBlock = allBlocks.FirstOrDefault(b => b.Id == childId);
            if (childBlock?.Text != null)
            {
                sb.Append(childBlock.Text).Append(" ");
            }
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Classifies document type based on content analysis
    /// </summary>
    private string ClassifyDocumentType(string extractedText)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
            return "Unknown";

        var textLower = extractedText.ToLowerInvariant();
        var scores = new Dictionary<string, int>();

        // Invoice detection
        if (Regex.IsMatch(textLower, @"invoice\s*number|inv\.|invoice date"))
            scores["Invoice"] = scores.GetValueOrDefault("Invoice", 0) + 3;
        if (textLower.Contains("subtotal") && textLower.Contains("tax"))
            scores["Invoice"] = scores.GetValueOrDefault("Invoice", 0) + 2;

        // Receipt detection
        if (Regex.IsMatch(textLower, @"receipt|transaction|thank you"))
            scores["Receipt"] = scores.GetValueOrDefault("Receipt", 0) + 3;

        // Medical report detection
        if (Regex.IsMatch(textLower, @"medical|diagnosis|patient|physician|treatment"))
            scores["MedicalReport"] = scores.GetValueOrDefault("MedicalReport", 0) + 3;

        // Insurance policy detection
        if (Regex.IsMatch(textLower, @"policy|coverage|premium|deductible|beneficiary"))
            scores["InsurancePolicy"] = scores.GetValueOrDefault("InsurancePolicy", 0) + 3;

        // Claim form detection
        if (Regex.IsMatch(textLower, @"claim form|claimant|date of loss|claim number"))
            scores["ClaimForm"] = scores.GetValueOrDefault("ClaimForm", 0) + 3;

        // Police report detection
        if (Regex.IsMatch(textLower, @"police|officer|incident|report number|witness"))
            scores["PoliceReport"] = scores.GetValueOrDefault("PoliceReport", 0) + 3;

        // Repair estimate detection
        if (Regex.IsMatch(textLower, @"estimate|repair|parts|labor|mechanic|damage"))
            scores["RepairEstimate"] = scores.GetValueOrDefault("RepairEstimate", 0) + 3;

        // Bank statement detection
        if (Regex.IsMatch(textLower, @"bank statement|account|balance|transaction"))
            scores["BankStatement"] = scores.GetValueOrDefault("BankStatement", 0) + 3;

        if (scores.Any())
        {
            var bestMatch = scores.OrderByDescending(s => s.Value).First();
            return bestMatch.Key;
        }

        return "Other";
    }

    /// <summary>
    /// Parses S3 URI in format s3://bucket/key
    /// </summary>
    private (string? bucket, string? key) ParseS3Uri(string uri)
    {
        if (uri.StartsWith("s3://", StringComparison.OrdinalIgnoreCase))
        {
            var parts = uri.Substring(5).Split('/', 2);
            if (parts.Length == 2)
                return (parts[0], parts[1]);
        }
        return (null, null);
    }
}

/// <summary>
/// Document intelligence result with extracted data
/// </summary>
public class DocumentIntelligenceResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string ExtractedText { get; set; } = string.Empty;
    public decimal TextConfidence { get; set; }
    public string DocumentType { get; set; } = "Unknown";
    public List<DocumentTable> Tables { get; set; } = new();
    public Dictionary<string, FormField> FormFields { get; set; } = new();
}

/// <summary>
/// Represents a table extracted from document
/// </summary>
public class DocumentTable
{
    public string TableId { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public List<List<string>> Rows { get; set; } = new();
}

/// <summary>
/// Represents a form field extracted from document
/// </summary>
public class FormField
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
}
