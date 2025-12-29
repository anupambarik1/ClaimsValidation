using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Claims.Services.Interfaces;
using Amazon.S3;
using Amazon.S3.Model;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System;
using System.Collections.Generic;

namespace Claims.Services.Aws;

/// <summary>
/// AWS S3 storage service integration.
/// Uses AWSSDK.S3 and default credential chain.
/// </summary>
public class AWSBlobStorageService : IBlobStorageService
{
    private readonly ILogger<AWSBlobStorageService> _logger;
    private readonly bool _isEnabled;
    private readonly string? _bucket;
    private readonly IAmazonS3 _s3Client;

    public AWSBlobStorageService(IConfiguration configuration, ILogger<AWSBlobStorageService> logger)
    {
        _logger = logger;
        _isEnabled = configuration.GetValue<bool>("AWS:Enabled", false);
        _bucket = configuration["AWS:S3Bucket"];

        if (!_isEnabled || string.IsNullOrEmpty(_bucket))
        {
            _logger.LogWarning("AWS S3 is disabled or S3Bucket not configured.");
        }

        _s3Client = new AmazonS3Client(); // uses default AWS credentials and region
    }

    public bool IsEnabled => _isEnabled && !string.IsNullOrEmpty(_bucket);

    public async Task<string?> UploadDocumentAsync(Guid claimId, string fileName, Stream content, string contentType = "application/octet-stream")
    {
        if (!IsEnabled) return null;

        var key = $"{claimId}/{Guid.NewGuid()}/{fileName}";
        try
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = content,
                ContentType = contentType
            };

            putRequest.Metadata.Add("ClaimId", claimId.ToString());
            var resp = await _s3Client.PutObjectAsync(putRequest);
            if (resp.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                var uri = $"s3://{_bucket}/{key}";
                _logger.LogInformation("Uploaded document to S3: {Uri}", uri);
                return uri;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading to S3");
        }

        return null;
    }

    public async Task<Stream?> DownloadDocumentAsync(string blobUri)
    {
        if (!IsEnabled) return null;
        var (bucket, key) = ParseS3Uri(blobUri);
        if (bucket == null || key == null) return null;

        try
        {
            var resp = await _s3Client.GetObjectAsync(bucket, key);
            var ms = new MemoryStream();
            await resp.ResponseStream.CopyToAsync(ms);
            ms.Position = 0;
            return ms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading S3 object {Uri}", blobUri);
            return null;
        }
    }

    public async Task<bool> DeleteDocumentAsync(string blobUri)
    {
        if (!IsEnabled) return false;
        var (bucket, key) = ParseS3Uri(blobUri);
        if (bucket == null || key == null) return false;

        try
        {
            var resp = await _s3Client.DeleteObjectAsync(bucket, key);
            return resp.HttpStatusCode == System.Net.HttpStatusCode.NoContent || resp.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting S3 object {Uri}", blobUri);
            return false;
        }
    }

    public async Task<List<BlobDocumentInfo>> GetClaimDocumentsAsync(Guid claimId)
    {
        var list = new List<BlobDocumentInfo>();
        if (!IsEnabled) return list;

        try
        {
            var prefix = $"{claimId}/";
            var request = new ListObjectsV2Request { BucketName = _bucket, Prefix = prefix };
            ListObjectsV2Response resp;
            do
            {
                resp = await _s3Client.ListObjectsV2Async(request);
                foreach (var o in resp.S3Objects)
                {
                    list.Add(new BlobDocumentInfo
                    {
                        Name = o.Key,
                        Uri = $"s3://{_bucket}/{o.Key}",
                        Size = o.Size,
                        ContentType = null,
                        UploadedAt = o.LastModified.ToUniversalTime()
                    });
                }
                request.ContinuationToken = resp.NextContinuationToken;
            } while (resp.IsTruncated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing S3 objects for claim {ClaimId}", claimId);
        }

        return list;
    }

    public async Task<string?> MoveToProcessedAsync(string rawBlobUri, Guid claimId)
    {
        if (!IsEnabled) return null;
        var (bucket, key) = ParseS3Uri(rawBlobUri);
        if (bucket == null || key == null) return null;

        try
        {
            var destKey = $"{claimId}/processed/{key.Split('/').Last()}";
            var copyReq = new CopyObjectRequest
            {
                SourceBucket = bucket,
                SourceKey = key,
                DestinationBucket = bucket,
                DestinationKey = destKey
            };
            await _s3Client.CopyObjectAsync(copyReq);
            await _s3Client.DeleteObjectAsync(bucket, key);
            return $"s3://{bucket}/{destKey}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving S3 object {Uri}", rawBlobUri);
            return null;
        }
    }

    private (string? bucket, string? key) ParseS3Uri(string uri)
    {
        // Accept formats s3://bucket/key or https://s3.../bucket/key
        if (uri.StartsWith("s3://", StringComparison.OrdinalIgnoreCase))
        {
            var parts = uri.Substring(5).Split('/', 2);
            if (parts.Length == 2) return (parts[0], parts[1]);
        }

        // Try to parse bucket from URL
        var m = Regex.Match(uri, @"https?://[^/]+/(?<bucket>[^/]+)/(?<key>.+)", RegexOptions.IgnoreCase);
        if (m.Success) return (m.Groups["bucket"].Value, m.Groups["key"].Value);

        return (null, null);
    }
}
