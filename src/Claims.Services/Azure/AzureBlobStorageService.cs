using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Claims.Services.Interfaces;

namespace Claims.Services.Azure;

/// <summary>
/// Azure Blob Storage service for document storage
/// </summary>
public class AzureBlobStorageService : Claims.Services.Interfaces.IBlobStorageService
{
    private readonly BlobServiceClient? _blobServiceClient;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly bool _isEnabled;
    private readonly string _rawDocsContainer;
    private readonly string _processedDocsContainer;

    public AzureBlobStorageService(
        IConfiguration configuration,
        ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;

        var connectionString = configuration["Azure:BlobStorage:ConnectionString"];
        _isEnabled = configuration.GetValue<bool>("Azure:BlobStorage:Enabled");
        _rawDocsContainer = configuration["Azure:BlobStorage:RawDocsContainer"] ?? "raw-docs";
        _processedDocsContainer = configuration["Azure:BlobStorage:ProcessedDocsContainer"] ?? "processed-docs";

        if (_isEnabled && !string.IsNullOrEmpty(connectionString))
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
            _logger.LogInformation("Azure Blob Storage initialized");
            
            // Ensure containers exist
            _ = InitializeContainersAsync();
        }
        else
        {
            _logger.LogWarning("Azure Blob Storage is disabled or not configured");
        }
    }

    public bool IsEnabled => _isEnabled && _blobServiceClient != null;

    private async Task InitializeContainersAsync()
    {
        if (_blobServiceClient == null) return;

        try
        {
            var rawContainer = _blobServiceClient.GetBlobContainerClient(_rawDocsContainer);
            await rawContainer.CreateIfNotExistsAsync(PublicAccessType.None);

            var processedContainer = _blobServiceClient.GetBlobContainerClient(_processedDocsContainer);
            await processedContainer.CreateIfNotExistsAsync(PublicAccessType.None);

            _logger.LogInformation("Blob containers initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing blob containers");
        }
    }

    /// <summary>
    /// Upload a document to Azure Blob Storage
    /// </summary>
    public async Task<string?> UploadDocumentAsync(
        Guid claimId,
        string fileName,
        Stream content,
        string contentType = "application/octet-stream")
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("Azure Blob Storage not enabled, skipping upload");
            return null;
        }

        try
        {
            var containerClient = _blobServiceClient!.GetBlobContainerClient(_rawDocsContainer);
            var blobName = $"{claimId}/{Guid.NewGuid()}/{fileName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = new Dictionary<string, string>
                {
                    { "ClaimId", claimId.ToString() },
                    { "OriginalFileName", fileName },
                    { "UploadedAt", DateTime.UtcNow.ToString("O") }
                }
            };

            await blobClient.UploadAsync(content, options);

            _logger.LogInformation("Document uploaded: {BlobName}", blobName);
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document to Azure Blob Storage");
            return null;
        }
    }

    /// <summary>
    /// Download a document from Azure Blob Storage
    /// </summary>
    public async Task<Stream?> DownloadDocumentAsync(string blobUri)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("Azure Blob Storage not enabled");
            return null;
        }

        try
        {
            var blobClient = new BlobClient(new Uri(blobUri));
            var response = await blobClient.DownloadStreamingAsync();
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document from Azure Blob Storage");
            return null;
        }
    }

    /// <summary>
    /// Get list of documents for a claim
    /// </summary>
    public async Task<List<BlobDocumentInfo>> GetClaimDocumentsAsync(Guid claimId)
    {
        var documents = new List<BlobDocumentInfo>();

        if (!IsEnabled)
        {
            return documents;
        }

        try
        {
            var containerClient = _blobServiceClient!.GetBlobContainerClient(_rawDocsContainer);
            var prefix = $"{claimId}/";

            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
            {
                documents.Add(new BlobDocumentInfo
                {
                    Name = blobItem.Name,
                    Uri = $"{containerClient.Uri}/{blobItem.Name}",
                    Size = blobItem.Properties.ContentLength ?? 0,
                    ContentType = blobItem.Properties.ContentType,
                    UploadedAt = blobItem.Properties.CreatedOn?.UtcDateTime
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing claim documents");
        }

        return documents;
    }

    /// <summary>
    /// Delete a document from Azure Blob Storage
    /// </summary>
    public async Task<bool> DeleteDocumentAsync(string blobUri)
    {
        if (!IsEnabled)
        {
            return false;
        }

        try
        {
            var blobClient = new BlobClient(new Uri(blobUri));
            await blobClient.DeleteIfExistsAsync();
            _logger.LogInformation("Document deleted: {Uri}", blobUri);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document");
            return false;
        }
    }

    /// <summary>
    /// Move document from raw to processed container
    /// </summary>
    public async Task<string?> MoveToProcessedAsync(string rawBlobUri, Guid claimId)
    {
        if (!IsEnabled)
        {
            return null;
        }

        try
        {
            var sourceBlobClient = new BlobClient(new Uri(rawBlobUri));
            var containerClient = _blobServiceClient!.GetBlobContainerClient(_processedDocsContainer);
            
            var destBlobName = $"{claimId}/{DateTime.UtcNow:yyyyMMdd}/{sourceBlobClient.Name.Split('/').Last()}";
            var destBlobClient = containerClient.GetBlobClient(destBlobName);

            // Copy blob
            await destBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);

            // Delete source
            await sourceBlobClient.DeleteIfExistsAsync();

            _logger.LogInformation("Document moved to processed: {Dest}", destBlobName);
            return destBlobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving document to processed");
            return null;
        }
    }
}

// Reuse BlobDocumentInfo defined in Claims.Services.Interfaces
