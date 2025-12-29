using Claims.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Amazon.Textract;
using Amazon.Textract.Model;
using Amazon.S3;
using System.Text;

namespace Claims.Services.Aws;

/// <summary>
/// AWS Textract OCR service implementation using AWSSDK.Textract.
/// Accepts S3 URIs (s3://bucket/key) and returns extracted text.
/// </summary>
public class AWSTextractService : IOcrService
{
    private readonly ILogger<AWSTextractService> _logger;
    private readonly bool _isEnabled;
    private readonly AmazonTextractClient _textractClient;

    public AWSTextractService(IConfiguration configuration, ILogger<AWSTextractService> logger)
    {
        _logger = logger;
        _isEnabled = configuration.GetValue<bool>("AWS:Enabled", false);
        _textractClient = new AmazonTextractClient();

        if (!_isEnabled)
            _logger.LogWarning("AWS Textract service is disabled in configuration.");
    }

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

            var req = new AnalyzeDocumentRequest
            {
                FeatureTypes = new List<string> { "TABLES", "FORMS" },
                Document = new Document
                {
                    S3Object = new S3Object { Bucket = bucket, Name = key }
                }
            };

            var resp = await _textractClient.AnalyzeDocumentAsync(req);
            var sb = new StringBuilder();
            decimal highestConfidence = 0m;
            foreach (var block in resp.Blocks)
            {
                if (block.BlockType == BlockType.LINE && !string.IsNullOrEmpty(block.Text))
                {
                    sb.AppendLine(block.Text);
                    if (block.Confidence > 0 && (decimal)block.Confidence > highestConfidence)
                        highestConfidence = (decimal)block.Confidence;
                }
            }

            return (sb.ToString(), highestConfidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Textract");
            return (string.Empty, 0m);
        }
    }

    public Task UpdateDocumentOcrStatusAsync(Guid documentId, string status, string? extractedText = null, decimal? confidence = null)
    {
        _logger.LogInformation("Updated OCR status for {DocumentId}: {Status} (confidence {Confidence})", documentId, status, confidence);
        return Task.CompletedTask;
    }

    private (string? bucket, string? key) ParseS3Uri(string uri)
    {
        if (uri.StartsWith("s3://", StringComparison.OrdinalIgnoreCase))
        {
            var parts = uri.Substring(5).Split('/', 2);
            if (parts.Length == 2) return (parts[0], parts[1]);
        }
        return (null, null);
    }
}
