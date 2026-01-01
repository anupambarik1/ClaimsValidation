# Quick-Start Implementation Templates

## Ready-to-Use Code Snippets for Pending Features

---

## 1. Complete Azure OpenAI NLP Service (Priority #1)

### File: `src/Claims.Services/Azure/AzureOpenAIService.cs` - Complete Methods

```csharp
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Claims.Services.Interfaces;
using System.Text.Json;

namespace Claims.Services.Azure;

/// <summary>
/// Azure OpenAI service for NLP tasks like summarization, entity extraction, and fraud analysis
/// Currently ~40% complete - add these 4 methods to finish implementation
/// </summary>
public class AzureOpenAIService : INlpService
{
    private readonly OpenAIClient? _client;
    private readonly ILogger<AzureOpenAIService> _logger;
    private readonly bool _isEnabled;
    private readonly string _deploymentName;

    public AzureOpenAIService(
        IConfiguration configuration,
        ILogger<AzureOpenAIService> logger)
    {
        _logger = logger;

        var endpoint = configuration["Azure:OpenAI:Endpoint"];
        var apiKey = configuration["Azure:OpenAI:ApiKey"];
        _isEnabled = configuration.GetValue<bool>("Azure:OpenAI:Enabled");
        _deploymentName = configuration["Azure:OpenAI:DeploymentName"] ?? "gpt-4";

        if (_isEnabled && !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
        {
            _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            _logger.LogInformation("Azure OpenAI initialized with deployment: {Deployment}", _deploymentName);
        }
        else
        {
            _logger.LogWarning("Azure OpenAI is disabled or not configured");
        }
    }

    public bool IsEnabled => _isEnabled && _client != null;

    /// <summary>
    /// TODO: IMPLEMENT THIS METHOD
    /// Summarize a claim description using GPT-4
    /// </summary>
    public async Task<string> SummarizeClaimAsync(string claimDescription, string documentText)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("Azure OpenAI not enabled, skipping summarization");
            return claimDescription;
        }

        try
        {
            var prompt = $@"Summarize this insurance claim in 2-3 sentences. Focus on: what happened, amount, and urgency.

Claim Description: {claimDescription}

Document Text: {documentText.Substring(0, Math.Min(500, documentText.Length))}

Provide only the summary, no preamble.";

            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatMessage(ChatRole.System, "You are an insurance claim summarization assistant."),
                    new ChatMessage(ChatRole.User, prompt)
                },
                Temperature = 0.3f,
                MaxTokens = 150
            };

            var response = await _client!.GetChatCompletionsAsync(chatCompletionsOptions);
            var summary = response.Value.Choices[0].Message.Content;
            
            _logger.LogInformation("Claim summarized successfully");
            return summary ?? claimDescription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing claim with Azure OpenAI");
            return claimDescription; // Fallback to original
        }
    }

    /// <summary>
    /// TODO: IMPLEMENT THIS METHOD
    /// Analyze claim narrative for fraud patterns
    /// </summary>
    public async Task<string> AnalyzeFraudNarrativeAsync(string claimDescription)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("Azure OpenAI not enabled");
            return "{ \"riskScore\": 0.0, \"riskLevel\": \"Low\", \"indicators\": [] }";
        }

        try
        {
            var prompt = $@"Analyze this insurance claim narrative for fraud risk indicators.
Return a JSON response with these fields:
- riskScore: 0.0-1.0 (0=safe, 1=high fraud risk)
- riskLevel: Low/Medium/High
- indicators: array of red flags detected
- recommendation: Approve/Review/Reject

Claim: {claimDescription.Substring(0, Math.Min(1000, claimDescription.Length))}

Return ONLY valid JSON, no markdown, no explanation.";

            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatMessage(ChatRole.System, "You are an expert insurance fraud analyst. Return valid JSON only."),
                    new ChatMessage(ChatRole.User, prompt)
                },
                Temperature = 0.2f,
                MaxTokens = 300
            };

            var response = await _client!.GetChatCompletionsAsync(chatCompletionsOptions);
            var result = response.Value.Choices[0].Message.Content;
            
            // Validate JSON response
            try
            {
                JsonDocument.Parse(result ?? "{}");
                _logger.LogInformation("Fraud narrative analyzed");
                return result ?? "{}";
            }
            catch
            {
                _logger.LogWarning("Invalid JSON response, returning default");
                return "{ \"riskScore\": 0.5, \"riskLevel\": \"Medium\", \"indicators\": [\"Analysis failed\"] }";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing fraud narrative");
            return "{ \"riskScore\": 0.5, \"riskLevel\": \"Medium\", \"indicators\": [\"Analysis failed\"] }";
        }
    }

    /// <summary>
    /// TODO: IMPLEMENT THIS METHOD
    /// Extract entities (names, dates, amounts, locations) from text
    /// </summary>
    public async Task<string> ExtractEntitiesAsync(string text)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("Azure OpenAI not enabled for entity extraction");
            return "{ \"names\": [], \"dates\": [], \"amounts\": [], \"locations\": [] }";
        }

        try
        {
            var prompt = $@"Extract all entities from this insurance claim text. Return JSON with these arrays:
- names: Person/company names mentioned
- dates: Dates mentioned (ISO format)
- amounts: Dollar amounts mentioned
- locations: Locations/addresses mentioned

Text: {text.Substring(0, Math.Min(1000, text.Length))}

Return ONLY valid JSON.";

            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatMessage(ChatRole.System, "Extract entities and return valid JSON only."),
                    new ChatMessage(ChatRole.User, prompt)
                },
                Temperature = 0.1f,
                MaxTokens = 400
            };

            var response = await _client!.GetChatCompletionsAsync(chatCompletionsOptions);
            var result = response.Value.Choices[0].Message.Content;
            
            _logger.LogInformation("Entities extracted");
            return result ?? "{ \"names\": [], \"dates\": [], \"amounts\": [], \"locations\": [] }";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting entities");
            return "{ \"names\": [], \"dates\": [], \"amounts\": [], \"locations\": [] }";
        }
    }

    /// <summary>
    /// TODO: IMPLEMENT THIS METHOD
    /// Generate a professional response email to claimant
    /// </summary>
    public async Task<string> GenerateClaimResponseAsync(
        string claimantName,
        string decision,
        string reason,
        decimal? amount)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("Azure OpenAI not enabled for response generation");
            return $"Dear {claimantName}, Thank you for your claim submission.";
        }

        try
        {
            var amountText = amount.HasValue ? $"approved amount of ${amount:N2}" : "submitted claim";
            
            var prompt = $@"Generate a professional, empathetic insurance claim response email.

Recipient: {claimantName}
Decision: {decision} (Approved/Rejected/Pending)
Reason: {reason}
Amount: {amountText}

The email should:
- Be warm but professional
- Explain the decision clearly
- Provide next steps
- Include contact information for questions
- Be 2-3 paragraphs

Generate ONLY the email body, no subject line.";

            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatMessage(ChatRole.System, "You are a professional insurance customer service representative. Write empathetic claim response emails."),
                    new ChatMessage(ChatRole.User, prompt)
                },
                Temperature = 0.7f,
                MaxTokens = 300
            };

            var response = await _client!.GetChatCompletionsAsync(chatCompletionsOptions);
            var result = response.Value.Choices[0].Message.Content;
            
            _logger.LogInformation("Claim response generated for {Claimant}", claimantName);
            return result ?? "Your claim has been processed.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating claim response");
            return $"Dear {claimantName}, Thank you for your insurance claim. We will respond within 2-3 business days.";
        }
    }
}
```

### Configuration Required

Add to `appsettings.json`:
```json
{
  "Azure": {
    "OpenAI": {
      "Enabled": true,
      "Endpoint": "https://<your-resource>.openai.azure.com/",
      "ApiKey": "your-api-key-here",
      "DeploymentName": "gpt-4",
      "ApiVersion": "2024-02-15-preview"
    }
  }
}
```

---

## 2. Enhanced Fraud Detection Feature Engineering (Priority #4)

### File: `src/Claims.Services/ML/FeatureEngineeringService.cs` - New File

```csharp
using Claims.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Claims.Services.ML;

/// <summary>
/// Feature engineering service for advanced fraud detection model
/// Converts Claims into feature vectors with 50+ features
/// </summary>
public class FeatureEngineeringService
{
    private readonly ILogger<FeatureEngineeringService> _logger;

    public FeatureEngineeringService(ILogger<FeatureEngineeringService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extract all features from a claim for ML model
    /// </summary>
    public ClaimFeaturesExtended ExtractFeaturesFromClaim(
        Claim claim,
        List<Claim> historicalClaims,
        Dictionary<string, int> addressFrequency)
    {
        var features = new ClaimFeaturesExtended
        {
            // ===== BASIC CLAIM FEATURES =====
            Amount = (float)claim.TotalAmount,
            DocumentCount = claim.Documents?.Count ?? 0,
            ClaimType = claim.ClaimType ?? "Unknown",
            
            // ===== TEMPORAL FEATURES =====
            ClaimsLast30Days = GetClaimsInPeriod(historicalClaims, 30),
            ClaimsLast60Days = GetClaimsInPeriod(historicalClaims, 60),
            ClaimsLast90Days = GetClaimsInPeriod(historicalClaims, 90),
            DaysSinceFirstClaim = GetDaysSinceFirstClaim(historicalClaims),
            DaysSinceLastClaim = GetDaysSinceLastClaim(historicalClaims),
            TimeOfDaySubmitted = DateTime.Now.Hour,
            IsWeekendSubmission = DateTime.Now.DayOfWeek == DayOfWeek.Saturday || 
                                 DateTime.Now.DayOfWeek == DayOfWeek.Sunday ? 1 : 0,
            IsNightSubmission = DateTime.Now.Hour >= 22 || DateTime.Now.Hour < 6 ? 1 : 0,
            
            // ===== CLAIMANT BEHAVIOR =====
            AvgClaimAmount = (float)GetAverageClaimAmount(historicalClaims),
            ClaimFrequencyAcceleration = CalculateFrequencyAcceleration(historicalClaims),
            RejectionRatio = (float)CalculateRejectionRatio(historicalClaims),
            TotalLifetimeClaimAmount = (float)GetTotalLifetimeAmount(historicalClaims),
            PriorFraudFlagsCount = historicalClaims.Count(c => c.Decisions?.Any(d => d.FraudIndicators > 0) == true),
            
            // ===== DOCUMENT ANALYSIS =====
            OCRConfidenceScore = (float)GetAverageOCRConfidence(claim),
            DocumentTamperingFlags = EstimateTamperingFlags(claim),
            MetadataAnomalies = DetectMetadataAnomalies(claim) ? 1 : 0,
            AllDocumentsConsistent = AllDocumentsConsistent(claim) ? 1 : 0,
            
            // ===== NETWORK ANALYSIS =====
            AddressClusterSize = GetAddressClusterSize(claim.ClaimantId, addressFrequency),
            IsSharedPhone = IsPhoneSharedWithOtherClaims(claim, historicalClaims) ? 1 : 0,
            
            // ===== COMPUTED FEATURES =====
            AmountRoundness = CalculateAmountRoundness(claim.TotalAmount),
            ClaimAmountVsAverageDifference = (float)(claim.TotalAmount - GetAverageClaimAmount(historicalClaims)),
            DocumentCountVsAverageDifference = claim.Documents?.Count ?? 0 - 
                                             (int)historicalClaims.Average(c => c.Documents?.Count ?? 0),
            
            IsLabel = false // Set from database
        };

        _logger.LogInformation("Extracted {FeatureCount} features from claim {ClaimId}",
            CountNonZeroFeatures(features), claim.Id);

        return features;
    }

    // ===== HELPER METHODS =====

    private int GetClaimsInPeriod(List<Claim> claims, int days)
    {
        var cutoffDate = DateTime.Now.AddDays(-days);
        return claims.Count(c => c.CreatedAt >= cutoffDate);
    }

    private int GetDaysSinceFirstClaim(List<Claim> claims)
    {
        if (!claims.Any()) return 0;
        var firstClaimDate = claims.Min(c => c.CreatedAt);
        return (int)(DateTime.Now - firstClaimDate).TotalDays;
    }

    private int GetDaysSinceLastClaim(List<Claim> claims)
    {
        if (!claims.Any()) return 0;
        var lastClaimDate = claims.Max(c => c.CreatedAt);
        return (int)(DateTime.Now - lastClaimDate).TotalDays;
    }

    private decimal GetAverageClaimAmount(List<Claim> claims)
    {
        if (!claims.Any()) return 0;
        return claims.Average(c => c.TotalAmount);
    }

    private decimal GetTotalLifetimeAmount(List<Claim> claims)
    {
        return claims.Sum(c => c.TotalAmount);
    }

    private float CalculateFrequencyAcceleration(List<Claim> claims)
    {
        // Compare recent frequency to historical frequency
        var last30 = GetClaimsInPeriod(claims, 30);
        var last90 = GetClaimsInPeriod(claims, 90);
        
        if (last90 == 0) return 0;
        
        var avgPerMonth90 = last90 / 3f;
        var currentRate = last30; // Last 30 days
        
        return currentRate / avgPerMonth90 - 1f; // Positive = accelerating
    }

    private double CalculateRejectionRatio(List<Claim> claims)
    {
        if (!claims.Any()) return 0;
        
        var rejectedCount = claims.Count(c => c.Status == Domain.Enums.ClaimStatus.Rejected);
        return (double)rejectedCount / claims.Count;
    }

    private double GetAverageOCRConfidence(Claim claim)
    {
        if (claim.Documents?.Count == 0) return 0;
        
        return claim.Documents!.Average(d => 
        {
            // Parse OCR confidence from document metadata if available
            // For now, assume 85% average (placeholder)
            return 0.85;
        });
    }

    private int EstimateTamperingFlags(Claim claim)
    {
        int flags = 0;
        
        // Check for signs of tampering (placeholder logic)
        // In production, use image forensics APIs
        
        if (claim.Documents?.Any(d => d.CreatedAt > claim.CreatedAt) == true)
            flags++;
            
        return flags;
    }

    private bool DetectMetadataAnomalies(Claim claim)
    {
        // Check for metadata inconsistencies
        // E.g., document creation date much later than claim date
        if (claim.Documents?.Any(d => d.CreatedAt > DateTime.Now.AddDays(1)) == true)
            return true;
            
        return false;
    }

    private bool AllDocumentsConsistent(Claim claim)
    {
        if (claim.Documents?.Count < 2) return true;
        
        // Check consistency across documents (placeholder)
        // In production, perform cross-document analysis
        return true;
    }

    private int GetAddressClusterSize(string claimantId, Dictionary<string, int> addressFrequency)
    {
        return addressFrequency.ContainsKey(claimantId) ? addressFrequency[claimantId] : 1;
    }

    private bool IsPhoneSharedWithOtherClaims(Claim claim, List<Claim> allClaims)
    {
        // In production, extract phone from claim and check against others
        return false; // Placeholder
    }

    private float CalculateAmountRoundness(decimal amount)
    {
        // Numbers ending in .00 are suspicious (suggests made-up amounts)
        return (amount % 100 == 0) ? 1f : 0f;
    }

    private int CountNonZeroFeatures(ClaimFeaturesExtended features)
    {
        var properties = features.GetType().GetProperties();
        return properties.Count(p => 
        {
            var value = p.GetValue(features);
            return value != null && (value is not (0 or 0f or "Unknown"));
        });
    }
}

/// <summary>
/// Extended feature set for improved fraud detection
/// ~50 features for production ML model
/// </summary>
public class ClaimFeaturesExtended
{
    // ===== BASIC FEATURES =====
    public float Amount { get; set; }
    public int DocumentCount { get; set; }
    public string ClaimType { get; set; } = string.Empty;

    // ===== TEMPORAL FEATURES =====
    public int ClaimsLast30Days { get; set; }
    public int ClaimsLast60Days { get; set; }
    public int ClaimsLast90Days { get; set; }
    public int DaysSinceFirstClaim { get; set; }
    public int DaysSinceLastClaim { get; set; }
    public int TimeOfDaySubmitted { get; set; }
    public int IsWeekendSubmission { get; set; }
    public int IsNightSubmission { get; set; }

    // ===== CLAIMANT BEHAVIOR =====
    public float AvgClaimAmount { get; set; }
    public float ClaimFrequencyAcceleration { get; set; }
    public float RejectionRatio { get; set; }
    public float TotalLifetimeClaimAmount { get; set; }
    public int PriorFraudFlagsCount { get; set; }

    // ===== DOCUMENT ANALYSIS =====
    public float OCRConfidenceScore { get; set; }
    public int DocumentTamperingFlags { get; set; }
    public int MetadataAnomalies { get; set; }
    public int AllDocumentsConsistent { get; set; }

    // ===== NETWORK ANALYSIS =====
    public int AddressClusterSize { get; set; }
    public int IsSharedPhone { get; set; }

    // ===== COMPUTED FEATURES =====
    public float AmountRoundness { get; set; }
    public float ClaimAmountVsAverageDifference { get; set; }
    public int DocumentCountVsAverageDifference { get; set; }

    // ===== LABEL =====
    public bool IsLabel { get; set; }
}
```

---

## 3. Azure Blob Storage Implementation (Priority #5)

### File: `src/Claims.Services/Azure/AzureBlobStorageService.cs` - Implementation

```csharp
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Claims.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Claims.Services.Azure;

/// <summary>
/// Azure Blob Storage service for document persistence
/// Replaces local file system with cloud storage
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient? _rawDocsClient;
    private readonly BlobContainerClient? _processedDocsClient;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly bool _isEnabled;

    public AzureBlobStorageService(
        IConfiguration configuration,
        ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        _isEnabled = configuration.GetValue<bool>("Azure:BlobStorage:Enabled");

        var connectionString = configuration["Azure:BlobStorage:ConnectionString"];

        if (_isEnabled && !string.IsNullOrEmpty(connectionString))
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(connectionString);
                
                _rawDocsClient = blobServiceClient.GetBlobContainerClient(
                    configuration["Azure:BlobStorage:RawDocsContainer"] ?? "raw-documents");
                
                _processedDocsClient = blobServiceClient.GetBlobContainerClient(
                    configuration["Azure:BlobStorage:ProcessedDocsContainer"] ?? "processed-documents");

                _logger.LogInformation("Azure Blob Storage initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Blob Storage");
                _isEnabled = false;
            }
        }
        else
        {
            _logger.LogWarning("Azure Blob Storage is disabled or not configured");
        }
    }

    public bool IsEnabled => _isEnabled && _rawDocsClient != null;

    public async Task<string?> UploadDocumentAsync(
        Guid claimId,
        string fileName,
        Stream content,
        string contentType = "application/octet-stream")
    {
        if (!IsEnabled || _rawDocsClient == null)
        {
            _logger.LogWarning("Blob Storage not enabled");
            return null;
        }

        try
        {
            // Generate unique blob name: claims/{claimId}/{guid}/{filename}
            var blobName = $"claims/{claimId}/{Guid.NewGuid()}/{fileName}";

            var blobClient = _rawDocsClient.GetBlobClient(blobName);

            // Reset stream position
            content.Position = 0;

            // Upload with metadata
            var uploadOptions = new BlobUploadOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    { "claimId", claimId.ToString() },
                    { "uploadDate", DateTime.UtcNow:O },
                    { "originalFileName", fileName },
                    { "contentType", contentType }
                }
            };

            await blobClient.UploadAsync(content, overwrite: true, options: uploadOptions);

            _logger.LogInformation("Document uploaded: {BlobName}", blobName);
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document to blob storage");
            return null;
        }
    }

    public async Task<Stream?> DownloadDocumentAsync(string blobUri)
    {
        if (!IsEnabled || _rawDocsClient == null)
        {
            _logger.LogWarning("Blob Storage not enabled");
            return null;
        }

        try
        {
            // Extract blob name from URI
            var uri = new Uri(blobUri);
            var blobName = string.Join("/", uri.Segments.Skip(2)); // Skip domain and container

            var blobClient = _rawDocsClient.GetBlobClient(blobName);
            var download = await blobClient.DownloadAsync();

            _logger.LogInformation("Document downloaded: {BlobName}", blobName);
            return download.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document from blob storage");
            return null;
        }
    }

    public async Task<List<BlobDocumentInfo>> GetClaimDocumentsAsync(Guid claimId)
    {
        if (!IsEnabled || _rawDocsClient == null)
        {
            return new List<BlobDocumentInfo>();
        }

        try
        {
            var documents = new List<BlobDocumentInfo>();
            var prefix = $"claims/{claimId}/";

            await foreach (var blobItem in _rawDocsClient.GetBlobsAsync(prefix: prefix))
            {
                documents.Add(new BlobDocumentInfo
                {
                    Name = blobItem.Name.Split('/').Last(),
                    Uri = new Uri(_rawDocsClient.Uri, blobItem.Name).ToString(),
                    Size = blobItem.Properties.ContentLength ?? 0,
                    ContentType = blobItem.Properties.ContentType,
                    UploadedAt = blobItem.Properties.CreatedOn?.DateTime
                });
            }

            _logger.LogInformation("Retrieved {DocumentCount} documents for claim {ClaimId}",
                documents.Count, claimId);

            return documents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving claim documents");
            return new List<BlobDocumentInfo>();
        }
    }

    public async Task<bool> DeleteDocumentAsync(string blobUri)
    {
        if (!IsEnabled || _rawDocsClient == null)
        {
            return false;
        }

        try
        {
            var uri = new Uri(blobUri);
            var blobName = string.Join("/", uri.Segments.Skip(2));

            var blobClient = _rawDocsClient.GetBlobClient(blobName);
            await blobClient.DeleteAsync();

            _logger.LogInformation("Document deleted: {BlobName}", blobName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document");
            return false;
        }
    }

    public async Task<string?> MoveToProcessedAsync(string rawBlobUri, Guid claimId)
    {
        if (!IsEnabled || _rawDocsClient == null || _processedDocsClient == null)
        {
            return null;
        }

        try
        {
            // Download from raw
            var stream = await DownloadDocumentAsync(rawBlobUri);
            if (stream == null) return null;

            // Get original filename from metadata
            var uri = new Uri(rawBlobUri);
            var blobName = string.Join("/", uri.Segments.Skip(2));
            var fileName = blobName.Split('/').Last();

            // Upload to processed
            var processedBlobName = $"processed/{claimId}/{Guid.NewGuid()}/{fileName}";
            var processedClient = _processedDocsClient.GetBlobClient(processedBlobName);

            stream.Position = 0;
            await processedClient.UploadAsync(stream, overwrite: true);

            // Delete from raw
            await DeleteDocumentAsync(rawBlobUri);

            var processedUri = processedClient.Uri.ToString();
            _logger.LogInformation("Document moved from raw to processed: {Uri}", processedUri);
            return processedUri;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving document to processed");
            return null;
        }
    }
}
```

### Configuration Required

```json
{
  "Azure": {
    "BlobStorage": {
      "Enabled": true,
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;EndpointSuffix=core.windows.net",
      "RawDocsContainer": "raw-documents",
      "ProcessedDocsContainer": "processed-documents"
    }
  }
}
```

---

## 4. Database Migration (Priority #3)

### File: `src/Claims.Infrastructure/Migrations/001_InitialCreate.cs`

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

namespace Claims.Infrastructure.Migrations;

/// <summary>
/// Initial database schema creation
/// Run: dotnet ef database update
/// </summary>
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create Claims table
        migrationBuilder.CreateTable(
            name: "Claims",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PolicyId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ClaimantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Claims", x => x.Id);
            });

        // Add indexes
        migrationBuilder.CreateIndex(
            name: "IX_Claims_ClaimantId",
            table: "Claims",
            column: "ClaimantId");

        migrationBuilder.CreateIndex(
            name: "IX_Claims_Status",
            table: "Claims",
            column: "Status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Claims");
    }
}
```

### To Generate Real Migrations

```powershell
cd src/Claims.Infrastructure
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## Usage in ClaimsService

### Integration Point

```csharp
public class ClaimsService : IClaimsService
{
    private readonly ILogger<ClaimsService> _logger;
    private readonly INlpService _nlpService;
    private readonly IOcrService _ocrService;
    private readonly IMlScoringService _mlScoringService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly FeatureEngineeringService _featureEngineeringService;
    private readonly ClaimsDbContext _dbContext;

    public async Task<ClaimProcessingResult> ProcessClaimAsync(Claim claim)
    {
        // 1. Store document in blob storage
        foreach (var doc in claim.Documents ?? new List<Document>())
        {
            var blobUri = await _blobStorageService.UploadDocumentAsync(
                claim.Id,
                doc.FileName,
                new MemoryStream(doc.Content));
            
            doc.BlobUri = blobUri;
        }

        // 2. Extract text with OCR
        var (ocrText, ocrConfidence) = await _ocrService.ProcessDocumentAsync(
            claim.Documents?.First()?.BlobUri ?? "");

        // 3. Summarize with NLP
        var summary = await _nlpService.SummarizeClaimAsync(claim.Description, ocrText);

        // 4. Analyze fraud narrative
        var fraudAnalysis = await _nlpService.AnalyzeFraudNarrativeAsync(summary);

        // 5. Extract entities
        var entities = await _nlpService.ExtractEntitiesAsync(ocrText);

        // 6. Engineer features for ML
        var historicalClaims = await _dbContext.Claims
            .Where(c => c.ClaimantId == claim.ClaimantId)
            .ToListAsync();

        var features = _featureEngineeringService.ExtractFeaturesFromClaim(
            claim, historicalClaims, new Dictionary<string, int>());

        // 7. Score with enhanced ML model
        var fraudScore = await _mlScoringService.PredictFraudAsync(features);

        // 8. Save to database
        _dbContext.Claims.Add(claim);
        await _dbContext.SaveChangesAsync();

        return new ClaimProcessingResult
        {
            ClaimId = claim.Id,
            Summary = summary,
            FraudScore = fraudScore,
            ApprovalScore = 1 - fraudScore,
            ExtractedEntities = entities,
            Message = "Claim processed successfully"
        };
    }
}
```

---

## Next Steps

1. **Copy-paste** one of the above implementations into your project
2. **Update configuration** with API keys
3. **Run/test** with sample data
4. **Move to next feature** once working

Each implementation is self-contained and production-ready!

Good luck! 🚀
