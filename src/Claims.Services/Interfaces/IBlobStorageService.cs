namespace Claims.Services.Interfaces;

public interface IBlobStorageService
{
    bool IsEnabled { get; }
    Task<string?> UploadDocumentAsync(Guid claimId, string fileName, Stream content, string contentType = "application/octet-stream");
    Task<Stream?> DownloadDocumentAsync(string blobUri);
    Task<List<BlobDocumentInfo>> GetClaimDocumentsAsync(Guid claimId);
    Task<bool> DeleteDocumentAsync(string blobUri);
    Task<string?> MoveToProcessedAsync(string rawBlobUri, Guid claimId);
}

public class BlobDocumentInfo
{
    public string Name { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? ContentType { get; set; }
    public DateTime? UploadedAt { get; set; }
}
