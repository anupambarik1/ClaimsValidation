using Claims.Services.Interfaces;
using Claims.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Amazon.Textract;
using Amazon.Textract.Model;
using Amazon;
using Amazon.Runtime;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Claims.Services.Aws;

/// <summary>
/// AWS Textract OCR service implementation using AWSSDK.Textract.
/// Accepts S3 URIs (s3://bucket/key) and returns extracted text with confidence scores.
/// Integrates with Claims.Infrastructure for document status tracking.
/// </summary>
public class AWSTextractService : IOcrService
{
    private readonly ILogger<AWSTextractService> _logger;
    private readonly bool _isEnabled;
    private readonly ClaimsDbContext _context;
    private readonly IAmazonTextract _textractClient;

    public AWSTextractService(
        IConfiguration configuration,
        ILogger<AWSTextractService> logger,
        ClaimsDbContext context)
    {
        _logger = logger;
        _context = context;
        _isEnabled = configuration.GetValue<bool>("AWS:Enabled", false);
        
        var accessKey = configuration["AWS:AccessKey"];
        var secretKey = configuration["AWS:SecretKey"];
        var regionName = configuration["AWS:Region"] ?? "us-east-1";
        
        AWSCredentials credentials = new BasicAWSCredentials(accessKey, secretKey);
        var region = RegionEndpoint.GetBySystemName(regionName);
        _textractClient = new AmazonTextractClient(credentials, region);

        if (!_isEnabled)
            _logger.LogWarning("AWS Textract service is disabled in configuration.");
    }

    /// <summary>
    /// Processes a document using AWS Textract
    /// Extracts text and confidence scores from S3-stored documents
    /// </summary>
    public async Task<(string ExtractedText, decimal Confidence)> ProcessDocumentAsync(string blobUri)
    {
        if (!_isEnabled)
        {
            _logger.LogWarning("Textract not enabled");
            return (string.Empty, 0m);
        }

        try
        {
            var (bucket, key) = ParseS3Uri(blobUri);
            if (bucket == null || key == null)
            {
                _logger.LogWarning("Invalid S3 URI for Textract: {Uri}", blobUri);
                return (string.Empty, 0m);
            }

            _logger.LogInformation("Processing document via Textract: s3://{Bucket}/{Key}", bucket, key);

            var request = new AnalyzeDocumentRequest
            {
                FeatureTypes = new List<string> { "TABLES", "FORMS" },
                Document = new Document
                {
                    S3Object = new S3Object { Bucket = bucket, Name = key }
                }
            };

            var response = await _textractClient.AnalyzeDocumentAsync(request);

            var extractedText = ExtractTextFromBlocks(response.Blocks, out var confidence);

            _logger.LogInformation(
                "Document processing complete - Confidence: {Confidence:P}, Length: {TextLength}",
                confidence, extractedText.Length);

            return (extractedText, confidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document with AWS Textract: {BlobUri}", blobUri);
            return (string.Empty, 0m);
        }
    }

    /// <summary>
    /// Updates document OCR status in database
    /// </summary>
    public async Task UpdateDocumentOcrStatusAsync(
        Guid documentId,
        string status,
        string? extractedText = null,
        decimal? confidence = null)
    {
        try
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);

            if (document == null)
            {
                _logger.LogWarning("Document not found: {DocumentId}", documentId);
                return;
            }

            // Update OCR status (would use enum if available)
            // document.OcrStatus = Enum.Parse<OcrStatus>(status);
            document.ExtractedText = extractedText;
            document.OcrConfidence = confidence;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Updated OCR status for document {DocumentId}: {Status} (confidence: {Confidence})",
                documentId, status, confidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating OCR status for document {DocumentId}", documentId);
        }
    }

    /// <summary>
    /// Extracts text from Textract response blocks
    /// Calculates average confidence from LINE blocks
    /// </summary>
    private string ExtractTextFromBlocks(List<Block> blocks, out decimal confidence)
    {
        var sb = new StringBuilder();
        var confidences = new List<decimal>();

        foreach (var block in blocks)
        {
            if (block.BlockType == BlockType.LINE && !string.IsNullOrEmpty(block.Text))
            {
                sb.AppendLine(block.Text);
                if (block.Confidence > 0)
                {
                    confidences.Add((decimal)block.Confidence);
                }
            }
        }

        confidence = confidences.Any() ? (decimal)confidences.Average() : 0m;
        return sb.ToString().Trim();
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
