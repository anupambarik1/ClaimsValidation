# AWS NLP Integration with Bedrock & Comprehend - Complete Implementation Guide

**Status**: Implementation Ready  
**Target Service**: AWS Bedrock (Claude 3) + AWS Comprehend  
**Estimated Time**: 4-6 hours  
**Complexity**: Medium-High

---

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Prerequisites & Setup](#prerequisites--setup)
3. [Step-by-Step Implementation](#step-by-step-implementation)
4. [Cost Analysis](#cost-analysis)
5. [Testing & Validation](#testing--validation)
6. [Integration with Claims Pipeline](#integration-with-claims-pipeline)
7. [Troubleshooting](#troubleshooting)

---

## Architecture Overview

### Current State
Your system already has:
- ✅ **AWSNlpService** (149 lines) - Skeleton using AWS Comprehend
- ✅ **INlpService Interface** - Well-defined with 4 key methods
- ✅ **DI Registration** - Configured in Program.cs based on feature flags
- ✅ **AWS Textract** - Document Intelligence working
- ✅ **AWS Credentials** - Configuration setup in place

### What We'll Add
```
User Submit Claim
    ↓
Claim Description + OCR Text
    ↓
AWS Bedrock (Claude 3) ← Text Analysis, Summarization
    ↓
AWS Comprehend ← Sentiment, Entity Extraction
    ↓
Fraud Risk Score + Entities + Summary
    ↓
ML Model + Rules Engine
    ↓
Auto Decision / Manual Review
```

### Service Responsibilities

**AWSNlpService** (Enhanced):
- **SummarizeClaimAsync()** → Bedrock Claude for intelligent summarization
- **AnalyzeFraudNarrativeAsync()** → Bedrock for fraud pattern detection + Comprehend for sentiment
- **ExtractEntitiesAsync()** → Comprehend for entities + Bedrock for claim type classification
- **GenerateClaimResponseAsync()** → Bedrock for personalized response generation

---

## Prerequisites & Setup

### 1. AWS Account Requirements

**Services Needed**:
- ✅ Amazon Bedrock (Claude 3 Haiku model)
- ✅ Amazon Comprehend
- ✅ IAM credentials with permissions

**IAM Policy** (add to your AWS user/role):
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "bedrock:InvokeModel",
        "bedrock:InvokeModelWithResponseStream"
      ],
      "Resource": "arn:aws:bedrock:us-east-1::foundation-model/anthropic.claude-3-5-haiku-20241022-v1:0"
    },
    {
      "Effect": "Allow",
      "Action": [
        "comprehend:DetectSentiment",
        "comprehend:DetectKeyPhrases",
        "comprehend:DetectEntities",
        "comprehend:DetectDominantLanguage"
      ],
      "Resource": "*"
    }
  ]
}
```

### 2. NuGet Packages

Your `Claims.Services.csproj` needs:
```xml
<ItemGroup>
    <PackageReference Include="AWSSDK.Bedrock" Version="3.7.100.37" />
    <PackageReference Include="AWSSDK.Comprehend" Version="3.7.400.5" />
    <PackageReference Include="AWSSDK.Core" Version="3.7.301.4" />
</ItemGroup>
```

**Install**:
```powershell
cd C:\Demo_Projects\Claim_Validation\src\Claims.Services
dotnet add package AWSSDK.Bedrock
dotnet add package AWSSDK.Comprehend
```

### 3. Configuration

Update `appsettings.json`:
```json
{
  "AWS": {
    "Enabled": true,
    "Region": "us-east-1",
    "AccessKey": "YOUR_ACCESS_KEY",
    "SecretKey": "YOUR_SECRET_KEY",
    "Bedrock": {
      "Enabled": true,
      "Model": "anthropic.claude-3-5-haiku-20241022-v1:0",
      "MaxTokens": 1024,
      "Temperature": 0.7
    },
    "Comprehend": {
      "Enabled": true
    }
  },
  "FeatureFlags": {
    "UseAWSBedrock": true
  }
}
```

**Available Models** (as of 2024):
- `anthropic.claude-3-5-haiku-20241022-v1:0` - Fast & cheap (recommended for MVP)
- `anthropic.claude-3-sonnet-20240229-v1:0` - Balanced
- `anthropic.claude-3-opus-20240229-v1:0` - Most powerful

---

## Step-by-Step Implementation

### Step 1: Update NuGet Packages

```powershell
cd C:\Demo_Projects\Claim_Validation
dotnet add Claims.Services/Claims.Services.csproj package AWSSDK.Bedrock
dotnet add Claims.Services/Claims.Services.csproj package AWSSDK.Comprehend
dotnet restore
```

### Step 2: Create AWS Bedrock Runtime Client Wrapper

Create `src/Claims.Services/Aws/AWSBedrockService.cs`:

```csharp
using Amazon;
using Amazon.Bedrock;
using Amazon.BedrockRuntime;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Claims.Services.Aws;

/// <summary>
/// Wrapper for AWS Bedrock Claude model invocation
/// Handles model invocation, prompt formatting, and response parsing
/// </summary>
public class AWSBedrockService
{
    private readonly ILogger<AWSBedrockService> _logger;
    private readonly IAmazonBedrockRuntime _bedrockClient;
    private readonly string _modelId;
    private readonly int _maxTokens;
    private readonly double _temperature;

    public AWSBedrockService(IConfiguration configuration, ILogger<AWSBedrockService> logger)
    {
        _logger = logger;
        
        var accessKey = configuration["AWS:AccessKey"];
        var secretKey = configuration["AWS:SecretKey"];
        var regionName = configuration["AWS:Region"] ?? "us-east-1";
        
        AWSCredentials credentials = new BasicAWSCredentials(accessKey, secretKey);
        var region = RegionEndpoint.GetBySystemName(regionName);
        
        _bedrockClient = new AmazonBedrockRuntimeClient(credentials, region);
        _modelId = configuration["AWS:Bedrock:Model"] ?? "anthropic.claude-3-5-haiku-20241022-v1:0";
        _maxTokens = configuration.GetValue<int>("AWS:Bedrock:MaxTokens", 1024);
        _temperature = configuration.GetValue<double>("AWS:Bedrock:Temperature", 0.7);
        
        _logger.LogInformation("Bedrock service initialized with model: {ModelId}", _modelId);
    }

    /// <summary>
    /// Invoke Claude model with given prompt
    /// Returns the text response from the model
    /// </summary>
    public async Task<string> InvokeClaudeAsync(string prompt)
    {
        try
        {
            var request = new InvokeModelRequest
            {
                ModelId = _modelId,
                Body = System.Text.Json.JsonSerializer.Serialize(new
                {
                    anthropic_version = "bedrock-2023-06-01",
                    max_tokens = _maxTokens,
                    temperature = _temperature,
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    }
                }),
                ContentType = "application/json"
            };

            request.Body.Seek(0, System.IO.SeekOrigin.Begin);
            
            var response = await _bedrockClient.InvokeModelAsync(request);
            
            using (var reader = new System.IO.StreamReader(response.Body))
            {
                var responseText = await reader.ReadToEndAsync();
                var jsonResponse = System.Text.Json.JsonDocument.Parse(responseText);
                
                var content = jsonResponse.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString();
                
                return content ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking Bedrock Claude model");
            throw;
        }
    }
}
```

### Step 3: Enhance AWSNlpService with Bedrock Integration

Replace the current `src/Claims.Services/Aws/AWSNlpService.cs`:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Claims.Services.Interfaces;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Amazon.Runtime;
using Amazon;
using System.Text.Json;

namespace Claims.Services.Aws;

/// <summary>
/// AWS NLP Service combining Bedrock (Claude 3) for text analysis
/// and AWS Comprehend for entity extraction & sentiment analysis
/// 
/// Features:
/// - Claim summarization with context awareness
/// - Fraud narrative analysis with pattern detection
/// - Named entity extraction (persons, dates, locations, amounts)
/// - Automated claim response generation
/// </summary>
public class AWSNlpService : INlpService
{
    private readonly ILogger<AWSNlpService> _logger;
    private readonly bool _isEnabled;
    private readonly AmazonComprehendClient _comprehendClient;
    private readonly AWSBedrockService _bedrockService;

    public AWSNlpService(IConfiguration configuration, ILogger<AWSNlpService> logger)
    {
        _logger = logger;
        _isEnabled = configuration.GetValue<bool>("AWS:Enabled", false);
        
        var accessKey = configuration["AWS:AccessKey"];
        var secretKey = configuration["AWS:SecretKey"];
        var regionName = configuration["AWS:Region"] ?? "us-east-1";
        
        AWSCredentials credentials = new BasicAWSCredentials(accessKey, secretKey);
        var region = RegionEndpoint.GetBySystemName(regionName);
        
        _comprehendClient = new AmazonComprehendClient(credentials, region);
        _bedrockService = new AWSBedrockService(configuration, logger);

        if (!_isEnabled)
        {
            _logger.LogWarning("AWS NLP service is disabled in configuration");
        }
    }

    public bool IsEnabled => _isEnabled;

    /// <summary>
    /// Summarize claim description and OCR text using Claude
    /// Extracts key information and creates concise summary
    /// </summary>
    public async Task<string> SummarizeClaimAsync(string claimDescription, string documentText)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("AWS NLP not enabled, returning original description");
            return claimDescription;
        }

        try
        {
            var combinedText = $"{claimDescription}\n\nExtracted Document Text:\n{documentText}";
            var truncated = combinedText.Substring(0, Math.Min(3000, combinedText.Length));

            var prompt = $@"You are an insurance claims analyst. Summarize this claim in 2-3 concise sentences.
Focus on: what happened, when, where, and claim amount if mentioned.
Keep summary under 100 words.

Claim Information:
{truncated}

Summary:";

            var summary = await _bedrockService.InvokeClaudeAsync(prompt);
            _logger.LogInformation("Claim summarized successfully");
            return summary.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing with Bedrock");
            return claimDescription;
        }
    }

    /// <summary>
    /// Analyze claim narrative for fraud indicators using Bedrock
    /// Returns JSON with risk score, level, and indicators
    /// </summary>
    public async Task<string> AnalyzeFraudNarrativeAsync(string claimDescription)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("AWS NLP not enabled for fraud analysis");
            return JsonSerializer.Serialize(new
            {
                riskScore = 0.0m,
                riskLevel = "Low",
                indicators = new List<string>(),
                recommendation = "Approve"
            });
        }

        try
        {
            var truncated = claimDescription.Substring(0, Math.Min(2000, claimDescription.Length));

            // Sentiment analysis from Comprehend
            var sentiment = await _comprehendClient.DetectSentimentAsync(new DetectSentimentRequest
            {
                Text = truncated,
                LanguageCode = "en"
            });

            // Fraud pattern detection from Bedrock
            var fraudPrompt = $@"You are a fraud detection specialist. Analyze this insurance claim for fraud indicators.
Look for: inconsistencies, vague details, unusual circumstances, known fraud patterns, unrealistic claims.

Rate the fraud risk on a scale of 0.0 (no risk) to 1.0 (high risk).

Claim Description:
{truncated}

Return ONLY a JSON object with:
{{
  ""riskScore"": <0.0 to 1.0>,
  ""riskLevel"": ""Low"" | ""Medium"" | ""High"",
  ""indicators"": [""indicator1"", ""indicator2"", ...],
  ""recommendation"": ""Approve"" | ""Review"" | ""Investigate""
}}";

            var fraudAnalysis = await _bedrockService.InvokeClaudeAsync(fraudPrompt);
            
            // Parse and merge sentiment data
            try
            {
                var parsed = JsonSerializer.Deserialize<JsonElement>(fraudAnalysis);
                var riskScore = parsed.GetProperty("riskScore").GetDouble();
                var sentimentBonus = sentiment.Sentiment == SentimentType.NEGATIVE ? 0.15 : 0.0;
                var adjustedScore = Math.Min(1.0, riskScore + sentimentBonus);

                var result = new
                {
                    riskScore = adjustedScore,
                    riskLevel = parsed.GetProperty("riskLevel").GetString(),
                    indicators = parsed.GetProperty("indicators"),
                    recommendation = parsed.GetProperty("recommendation").GetString(),
                    sentimentAnalysis = sentiment.Sentiment.Value
                };

                _logger.LogInformation("Fraud analysis completed. Risk: {RiskLevel}", result.riskLevel);
                return JsonSerializer.Serialize(result);
            }
            catch (Exception parseEx)
            {
                _logger.LogWarning(parseEx, "Could not parse Bedrock response, returning raw");
                return fraudAnalysis;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing fraud narrative");
            return JsonSerializer.Serialize(new
            {
                riskScore = 0.5m,
                riskLevel = "Medium",
                indicators = new[] { "Analysis failed, defaulting to medium risk" },
                recommendation = "Review"
            });
        }
    }

    /// <summary>
    /// Extract entities from claim text using Comprehend + Bedrock
    /// Returns persons, dates, locations, amounts, and inferred claim type
    /// </summary>
    public async Task<string> ExtractEntitiesAsync(string text)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("AWS NLP not enabled for entity extraction");
            return JsonSerializer.Serialize(new
            {
                names = new List<string>(),
                dates = new List<string>(),
                amounts = new List<decimal>(),
                locations = new List<string>(),
                claimType = "unknown"
            });
        }

        try
        {
            var truncated = text.Substring(0, Math.Min(500, text.Length));

            // Use Comprehend for entity detection
            var entities = await _comprehendClient.DetectEntitiesAsync(new DetectEntitiesRequest
            {
                Text = truncated,
                LanguageCode = "en"
            });

            var names = entities.Entities.Where(e => e.Type == "PERSON").Select(e => e.Text).ToList();
            var dates = entities.Entities.Where(e => e.Type == "DATE").Select(e => e.Text).ToList();
            var locations = entities.Entities.Where(e => e.Type == "LOCATION").Select(e => e.Text).ToList();

            // Use Bedrock to classify claim type and extract amounts
            var classificationPrompt = $@"Classify this insurance claim and extract the claim amount.

Text:
{truncated}

Return ONLY JSON with:
{{
  ""claimType"": ""medical"" | ""auto"" | ""property"" | ""life"" | ""disability"" | ""other"",
  ""amounts"": [list of numeric amounts mentioned]
}}";

            var classification = await _bedrockService.InvokeClaudeAsync(classificationPrompt);
            
            try
            {
                var parsed = JsonSerializer.Deserialize<JsonElement>(classification);
                var amounts = parsed.GetProperty("amounts")
                    .EnumerateArray()
                    .Select(a => Convert.ToDecimal(a.GetDouble()))
                    .ToList();
                var claimType = parsed.GetProperty("claimType").GetString();

                var result = new
                {
                    names = names,
                    dates = dates,
                    amounts = amounts,
                    locations = locations,
                    claimType = claimType
                };

                _logger.LogInformation("Entities extracted: {Count} names, {DateCount} dates, {AmountCount} amounts", 
                    names.Count, dates.Count, amounts.Count);
                
                return JsonSerializer.Serialize(result);
            }
            catch
            {
                return JsonSerializer.Serialize(new
                {
                    names = names,
                    dates = dates,
                    amounts = new List<decimal>(),
                    locations = locations,
                    claimType = "unknown"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting entities");
            return JsonSerializer.Serialize(new
            {
                names = new List<string>(),
                dates = new List<string>(),
                amounts = new List<decimal>(),
                locations = new List<string>(),
                claimType = "unknown"
            });
        }
    }

    /// <summary>
    /// Generate personalized claim response using Bedrock
    /// Creates professional response letter with decision details
    /// </summary>
    public async Task<string> GenerateClaimResponseAsync(
        string claimantName,
        string decision,
        string reason,
        decimal? amount)
    {
        if (!IsEnabled)
        {
            return GenerateTemplateResponse(claimantName, decision, reason, amount);
        }

        try
        {
            var prompt = $@"Write a professional insurance claim decision letter.

Claimant: {claimantName}
Decision: {decision}
Reason: {reason}
Approved Amount: {(amount.HasValue ? $"${amount.Value:F2}" : "N/A")}

The letter should:
- Be professional and empathetic
- Clearly state the decision
- Explain the reason
- Include next steps
- Keep tone appropriate for the decision
- Be 2-3 paragraphs

Letter:";

            var response = await _bedrockService.InvokeClaudeAsync(prompt);
            _logger.LogInformation("Claim response generated for {ClaimantName}", claimantName);
            return response.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response with Bedrock, using template");
            return GenerateTemplateResponse(claimantName, decision, reason, amount);
        }
    }

    private static string GenerateTemplateResponse(
        string claimantName,
        string decision,
        string reason,
        decimal? amount)
    {
        return $@"Dear {claimantName},

Thank you for submitting your insurance claim. After careful review, your claim has been {decision.ToLower()}.

Reason: {reason}

{(amount.HasValue ? $"Approved Amount: ${amount.Value:F2}\n" : "")}
If you have any questions or would like to appeal this decision, please contact our claims support team.

Best regards,
Claims Validation System
Insurance Company";
    }
}
```

### Step 4: Update Program.cs Registration

Update the NLP service registration in `src/Claims.Api/Program.cs`:

```csharp
// Register NLP service - prefer AWS Bedrock if both enabled
var useAzureOpenAI = builder.Configuration.GetValue<bool>("FeatureFlags:UseAzureOpenAI");
var useAwsBedrock = builder.Configuration.GetValue<bool>("AWS:Bedrock:Enabled");
var awsEnabled = builder.Configuration.GetValue<bool>("AWS:Enabled");

if (awsEnabled && useAwsBedrock)
{
    builder.Services.AddSingleton<INlpService, AWSNlpService>();
    builder.Services.AddSingleton<AWSBedrockService>();
}
else if (useAzureOpenAI)
{
    builder.Services.AddSingleton<INlpService, AzureOpenAIService>();
}
else if (awsEnabled)
{
    builder.Services.AddSingleton<INlpService, AWSNlpService>();
}
else
{
    builder.Services.AddSingleton<INlpService, AzureOpenAIService>();
}
```

### Step 5: Integrate with ClaimsService

Update `src/Claims.Services/Implementations/ClaimsService.cs` to use NLP in the pipeline:

Add NLP dependency injection:
```csharp
private readonly INlpService _nlpService;

public ClaimsService(
    IClaimsService claimsService,  // existing
    IDocumentAnalysisService documentAnalysisService,  // existing
    IRulesEngineService rulesEngineService,  // existing
    IMlScoringService mlScoringService,  // existing
    INotificationService notificationService,  // existing
    INlpService nlpService)  // ADD THIS
{
    _nlpService = nlpService;
    // ... rest of constructor
}
```

Add NLP processing in `ProcessClaimAsync()`:
```csharp
// After Step 1 (OCR) and Step 2 (Document Analysis)
// Add Step 2.5: NLP Analysis
_logger.LogInformation("Step 2.5: NLP Analysis");

// Summarize claim
var summary = await _nlpService.SummarizeClaimAsync(
    claim.Description ?? string.Empty,
    ocrResults.FirstOrDefault()?.ExtractedText ?? string.Empty);

claim.Description = summary;  // Update with summary

// Analyze narrative for fraud
var fraudNarrativeJson = await _nlpService.AnalyzeFraudNarrativeAsync(summary);
var fraudNarrative = JsonSerializer.Deserialize<JsonElement>(fraudNarrativeJson);
var narrativeFraudScore = (decimal)fraudNarrative.GetProperty("riskScore").GetDouble();

// Extract entities
var entitiesJson = await _nlpService.ExtractEntitiesAsync(ocrResults.FirstOrDefault()?.ExtractedText ?? string.Empty);
var entities = JsonSerializer.Deserialize<JsonElement>(entitiesJson);

result.NlpAnalysis = new NlpAnalysisResult
{
    Summary = summary,
    FraudRiskScore = narrativeFraudScore,
    DetectedEntities = entitiesJson,
    ClaimType = entities.GetProperty("claimType").GetString()
};

// Continue with Steps 3-5 as before...
```

---

## Cost Analysis

### Claude 3 Haiku Pricing (Recommended for MVP)
- **Input**: $0.25 per million tokens
- **Output**: $1.25 per million tokens
- **Typical claim**: ~500 input tokens, ~200 output tokens
- **Cost per claim**: ~$0.00035

### AWS Comprehend Pricing
- **Per unit** (100 characters): $0.0001
- **Typical claim**: ~1000 characters
- **Cost per claim**: ~$0.0001

### Monthly Estimate (1000 claims/month)
- Bedrock: $0.35
- Comprehend: $0.10
- **Total NLP**: ~$0.45/month
- Textract (already implemented): ~$15-30/month

**Total AI Stack**: ~$15-30/month for MVP

---

## Testing & Validation

### Unit Test Example

Create `src/Claims.Services.Tests/Aws/AWSNlpServiceTests.cs`:

```csharp
[TestClass]
public class AWSNlpServiceTests
{
    [TestMethod]
    public async Task SummarizeClaimAsync_WithValidText_ReturnsSummary()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "AWS:Enabled", "true" },
                { "AWS:Region", "us-east-1" },
                { "AWS:AccessKey", "test" },
                { "AWS:SecretKey", "test" }
            })
            .Build();

        var logger = new Mock<ILogger<AWSNlpService>>();
        var service = new AWSNlpService(configuration, logger.Object);

        // Act
        var result = await service.SummarizeClaimAsync(
            "Car accident on Main St",
            "Police report filed. Damages estimated at $5000");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    public async Task AnalyzeFraudNarrativeAsync_WithHighRiskText_ReturnsHighScore()
    {
        // Test with suspicious claim description
        // Verify fraud score > 0.5
    }

    [TestMethod]
    public async Task ExtractEntitiesAsync_WithClaimText_ReturnsEntities()
    {
        // Test entity extraction
        // Verify names, dates, amounts detected
    }
}
```

### Integration Test
```csharp
[TestMethod]
public async Task ProcessClaim_WithNlp_EnhancesDecisionAccuracy()
{
    // Create test claim with narrative
    // Process through full pipeline
    // Verify NLP results incorporated into decision
}
```

### Manual Testing Checklist
- [ ] Summarization: Creates concise, accurate summaries
- [ ] Fraud Detection: High-risk claims flagged appropriately
- [ ] Entity Extraction: Correctly identifies amounts, dates, names
- [ ] Response Generation: Professional, relevant claim responses
- [ ] Performance: Claims processed in < 3 seconds
- [ ] Cost: Monthly bill < $50 for test volume

---

## Integration with Claims Pipeline

### Modified ProcessClaimAsync Flow

```
1. Retrieve Claim from DB
2. OCR Document (Textract) ✓ Already implemented
3. Document Analysis (Textract + Classification) ✓ Already implemented
4. ← NEW: NLP Analysis (Bedrock + Comprehend)
   ├─ Summarize claim narrative
   ├─ Analyze fraud patterns
   ├─ Extract entities
   └─ Detect claim type
5. Rules Engine Validation ✓ Already implemented
6. ML Scoring + NLP fraud signals combined ✓ Update existing
7. Decision Engine ✓ Already implemented
8. Generate Response (Bedrock) ← ENHANCED
9. Store Results & Notify ✓ Already implemented
```

### Code Integration Points

**In ClaimsService.ProcessClaimAsync():**
```csharp
// Step 2.5: NLP Analysis (NEW)
var summary = await _nlpService.SummarizeClaimAsync(claim.Description, ocrText);
var fraudAnalysis = await _nlpService.AnalyzeFraudNarrativeAsync(summary);
var entities = await _nlpService.ExtractEntitiesAsync(ocrText);

// Step 4: Enhanced ML Scoring (MODIFIED)
var nlpFraudScore = ExtractScoreFromJson(fraudAnalysis);
var combinedFraudScore = (fraudScore * 0.6) + (nlpFraudScore * 0.4);  // Weight both signals

// Step 8: Generate Response (ENHANCED)
var response = await _nlpService.GenerateClaimResponseAsync(
    claim.ClaimantName,
    decision,
    decisionReason,
    approvedAmount);
```

---

## Troubleshooting

### Error: "Unable to invoke model"
**Cause**: Model access not enabled in Bedrock  
**Fix**: Go to AWS Console → Bedrock → Model Access → Request access to Claude 3 models

### Error: "InvalidParameter: Invalid model identifier"
**Cause**: Model name doesn't match AWS format  
**Fix**: Use exact model ID from AWS: `anthropic.claude-3-5-haiku-20241022-v1:0`

### Error: "Access Denied" on Comprehend
**Cause**: IAM policy missing Comprehend permissions  
**Fix**: Add permissions from "Prerequisites" section above

### High API costs
**Cause**: Using Claude 3 Opus instead of Haiku  
**Fix**: Switch to Haiku model (much cheaper, sufficient for MVP)

### Slow response times
**Cause**: Large token limits or streaming processing  
**Fix**: Reduce `MaxTokens` from 2048 to 512, truncate input text

### JSON parsing errors from Bedrock
**Cause**: Claude returning natural language instead of JSON  
**Fix**: Make prompt more explicit: "Return ONLY valid JSON"

---

## Next Steps After Implementation

1. **Monitor Costs**
   - CloudWatch dashboard for API calls
   - Set billing alerts at $20/month

2. **Optimize Prompts**
   - Test different system prompts
   - Fine-tune temperature/token settings
   - A/B test response quality

3. **Enhance ML Model**
   - Include NLP fraud scores in training data
   - Retrain with 6-12 months of data
   - Measure accuracy improvement

4. **Implement Caching**
   - Cache summaries for identical claims
   - Reduce API calls by 20-30%

5. **Add Monitoring**
   - Track fraud detection accuracy
   - Monitor response generation quality
   - Set up alerts for model failures

---

## Estimated Implementation Timeline

| Phase | Task | Duration |
|-------|------|----------|
| 1 | Install NuGet packages | 15 min |
| 2 | Create AWSBedrockService | 30 min |
| 3 | Enhance AWSNlpService | 60 min |
| 4 | Update Program.cs | 15 min |
| 5 | Integrate with ClaimsService | 45 min |
| 6 | Configure appsettings.json | 10 min |
| 7 | Unit & integration testing | 90 min |
| 8 | Manual testing & validation | 60 min |
| **Total** | | **4.5-5.5 hours** |

---

## Success Criteria

✅ **Code**
- [ ] AWSBedrockService created and working
- [ ] AWSNlpService enhanced with Bedrock integration
- [ ] All 4 methods (Summarize, Analyze, Extract, Generate) functional
- [ ] No compilation errors
- [ ] All unit tests passing

✅ **Functional**
- [ ] Claims processed with NLP analysis in < 3 seconds
- [ ] Fraud scores accurate within expected range
- [ ] Entities correctly extracted from documents
- [ ] Responses professionally generated

✅ **Operational**
- [ ] Configuration via appsettings.json working
- [ ] AWS credentials properly secured
- [ ] Logging shows all NLP operations
- [ ] Cost tracking < $50/month for test volume

---

## References

- [AWS Bedrock Documentation](https://docs.aws.amazon.com/bedrock)
- [Claude 3 Models](https://www.anthropic.com/news/claude-3-family)
- [AWS Comprehend API](https://docs.aws.amazon.com/comprehend)
- [AWSSDK.Bedrock NuGet](https://www.nuget.org/packages/AWSSDK.Bedrock)
- [AWSSDK.Comprehend NuGet](https://www.nuget.org/packages/AWSSDK.Comprehend)

