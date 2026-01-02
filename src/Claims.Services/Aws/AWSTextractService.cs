using Claims.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Amazon.Textract;
using Amazon.Textract.Model;
using Amazon.S3;
using Amazon;
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
        _isEnabled = configuration.GetValue<bool>("AWS:Textract:Enabled", false);
        
        // Get AWS credentials and region from configuration
        var awsConfig = configuration.GetSection("AWS");
        var accessKey = awsConfig.GetValue<string>("AccessKey");
        var secretKey = awsConfig.GetValue<string>("SecretKey");
        var region = awsConfig.GetValue<string>("Region", "us-east-1");

        try
        {
            // Create client with explicit credentials and region
            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
                _textractClient = new AmazonTextractClient(credentials, RegionEndpoint.GetBySystemName(region));
                _logger.LogInformation("AWS Textract client initialized with credentials for region: {Region}", region);
            }
            else
            {
                // Fallback to default credentials chain (IAM role, environment variables, etc.)
                _textractClient = new AmazonTextractClient(RegionEndpoint.GetBySystemName(region));
                _logger.LogInformation("AWS Textract client initialized with default credentials for region: {Region}", region);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing AWS Textract client");
            _textractClient = new AmazonTextractClient();
        }

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

            _logger.LogInformation("Textract: Attempting to process S3 object - Bucket: {Bucket}, Key: {Key}", bucket, key);

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

            _logger.LogInformation("Textract: Successfully processed document, extracted {LineCount} lines", 
                resp.Blocks.Count(b => b.BlockType == BlockType.LINE));
            return (sb.ToString(), highestConfidence);
        }
        catch (Amazon.Textract.Model.InvalidS3ObjectException ex)
        {
            _logger.LogError(ex, "Textract S3 Error - Unable to access S3 object. Check: 1) S3 bucket exists in correct region, 2) Object key is correct, 3) IAM permissions allow Textract access, 4) AWS credentials are valid");
            return (string.Empty, 0m);
        }
        catch (Amazon.Runtime.Internal.HttpErrorResponseException ex)
        {
            _logger.LogError(ex, "Textract HTTP Error - AWS service responded with error. StatusCode: {StatusCode}", ex.Message);
            return (string.Empty, 0m);
        }
        catch (AmazonTextractException ex)
        {
            _logger.LogError(ex, "Textract AWS Service Error: {Message}", ex.Message);
            return (string.Empty, 0m);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Textract for URI: {Uri}", blobUri);
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
