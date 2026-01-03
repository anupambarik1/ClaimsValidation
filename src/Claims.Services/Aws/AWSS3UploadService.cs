using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Claims.Services.Aws;

/// <summary>
/// AWS S3 service for uploading claim documents
/// </summary>
public interface IS3UploadService
{
    Task<string> UploadDocumentAsync(string filePath, string claimId, string documentId);
}

public class AWSS3UploadService : IS3UploadService
{
    private readonly ILogger<AWSS3UploadService> _logger;
    private readonly string _bucketName;
    private readonly AmazonS3Client _s3Client;

    public AWSS3UploadService(IConfiguration configuration, ILogger<AWSS3UploadService> logger)
    {
        _logger = logger;
        _bucketName = configuration.GetValue<string>("AWS:S3Bucket") ?? "claims-documents-validation";

        // Get AWS credentials and region from configuration
        var awsConfig = configuration.GetSection("AWS");
        var accessKey = awsConfig.GetValue<string>("AccessKey");
        var secretKey = awsConfig.GetValue<string>("SecretKey");
        var region = awsConfig.GetValue<string>("Region", "us-east-1");

        try
        {
            // Create S3 client with explicit credentials and region
            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                var credentials = new BasicAWSCredentials(accessKey, secretKey);
                _s3Client = new AmazonS3Client(credentials, RegionEndpoint.GetBySystemName(region));
                _logger.LogInformation("S3 client initialized with credentials for region: {Region}, bucket: {Bucket}", region, _bucketName);
            }
            else
            {
                // Fallback to default credentials chain
                _s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(region));
                _logger.LogInformation("S3 client initialized with default credentials for region: {Region}, bucket: {Bucket}", region, _bucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing S3 client");
            throw;
        }
    }

    /// <summary>
    /// Uploads a document from local file to S3 and returns the S3 URI
    /// </summary>
    /// <param name="filePath">Local file path to upload</param>
    /// <param name="claimId">Claim ID for organizing documents</param>
    /// <param name="documentId">Document ID for unique naming</param>
    /// <returns>S3 URI in format s3://bucket/path</returns>
    public async Task<string> UploadDocumentAsync(string filePath, string claimId, string documentId)
    {
        try
        {
            // Check if local file exists
            if (!File.Exists(filePath))
            {
                _logger.LogError("Local file not found: {FilePath}", filePath);
                throw new FileNotFoundException($"Document file not found: {filePath}");
            }

            // Get file info
            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileName(filePath);
            
            // Create S3 object key: claims/{claimId}/{documentId}/{fileName}
            var s3Key = $"claims/{claimId}/{documentId}/{fileName}";

            _logger.LogInformation("Uploading document to S3: {FilePath} -> s3://{Bucket}/{S3Key}", filePath, _bucketName, s3Key);

            // Read file and upload to S3
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var uploadRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = s3Key,
                    InputStream = fileStream,
                    ContentType = GetContentType(fileName)
                };
                
                uploadRequest.Metadata.Add("claim-id", claimId);
                uploadRequest.Metadata.Add("document-id", documentId);
                uploadRequest.Metadata.Add("original-filename", fileName);
                uploadRequest.Metadata.Add("upload-date", DateTime.UtcNow.ToString("O"));

                var response = await _s3Client.PutObjectAsync(uploadRequest);
                
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    var s3Uri = $"s3://{_bucketName}/{s3Key}";
                    _logger.LogInformation("Document successfully uploaded to S3: {S3Uri}, ETag: {ETag}", s3Uri, response.ETag);
                    return s3Uri;
                }
                else
                {
                    _logger.LogError("S3 upload failed with status: {StatusCode}", response.HttpStatusCode);
                    throw new Exception($"S3 upload failed: {response.HttpStatusCode}");
                }
            }
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "AWS S3 error uploading document: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document to S3: {FilePath}", filePath);
            throw;
        }
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }
}
