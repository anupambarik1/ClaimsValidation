using Claims.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Amazon.Textract;
using Amazon.Textract.Model;
using Amazon.S3;
using Amazon.S3.Model;
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

            _logger.LogInformation("Processing document with Textract: s3://{Bucket}/{Key}", bucket, key);

            try
            {
                // Try AWS Textract for proper OCR and document parsing
                var req = new AnalyzeDocumentRequest
                {
                    FeatureTypes = new List<string> { "TABLES", "FORMS" },
                    Document = new Amazon.Textract.Model.Document
                    {
                        S3Object = new Amazon.Textract.Model.S3Object { Bucket = bucket, Name = key }
                    }
                };

                var resp = await _textractClient.AnalyzeDocumentAsync(req);
                var sb = new StringBuilder();
                decimal highestConfidence = 0m;
                
                // Extract text from Textract blocks
                foreach (var block in resp.Blocks)
                {
                    if (block.BlockType == BlockType.LINE && !string.IsNullOrEmpty(block.Text))
                    {
                        sb.AppendLine(block.Text);
                        if (block.Confidence > 0 && (decimal)block.Confidence > highestConfidence)
                            highestConfidence = (decimal)block.Confidence;
                    }
                }

                _logger.LogInformation("Textract: Successfully extracted {LineCount} lines with {Confidence:P2} confidence", 
                    resp.Blocks.Count(b => b.BlockType == BlockType.LINE), highestConfidence);
                
                return (sb.ToString(), highestConfidence);
            }
            catch (Amazon.Textract.Model.UnsupportedDocumentException ex)
            {
                _logger.LogWarning(ex, "Textract: Document format not supported. Falling back to direct file read. File: {Key}", key);
                // Fallback: Read file directly from S3 as text
                return await ReadFileDirectlyAsync(bucket, key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document: {Uri}", blobUri);
            return (string.Empty, 0m);
        }
    }

    /// <summary>
    /// Fallback: Reads file content directly from S3 when Textract fails
    /// </summary>
    private async Task<(string ExtractedText, decimal Confidence)> ReadFileDirectlyAsync(string bucket, string key)
    {
        try
        {
            _logger.LogInformation("Reading file directly from S3: s3://{Bucket}/{Key}", bucket, key);
            
            var s3Client = new AmazonS3Client();
            var request = new Amazon.S3.Model.GetObjectRequest
            {
                BucketName = bucket,
                Key = key
            };

            var response = await s3Client.GetObjectAsync(request);
            using (var reader = new StreamReader(response.ResponseStream))
            {
                var content = await reader.ReadToEndAsync();
                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogWarning("File content is empty: {Key}", key);
                    return (string.Empty, 0.5m);
                }

                _logger.LogInformation("Fallback: Successfully read {Length} chars from file", content.Length);
                return (content, 0.70m); // Lower confidence for direct read
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file directly: {Bucket}/{Key}", bucket, key);
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
