# AWS NLP Integration - Complete Execution Plan

**Project**: Claims Validation System  
**Feature**: End-to-End NLP with AWS Bedrock (Claude 3) + Comprehend  
**Timeline**: 4-5 hours  
**Status**: Ready to Execute

---

## Overview

This document contains:
1. Complete architecture and design decisions
2. Step-by-step implementation with code
3. Exact files to create/modify
4. Testing strategy
5. Integration points

---

## Architecture

### Current State
```
Claim PDF → Textract (OCR) ✅ → Document Analysis ✅ → Rules Engine ✅ 
→ ML Scoring ✅ → Decision ✅ → Template Email
```

### Target State
```
Claim PDF + Description → Textract (OCR) → Document Analysis 
→ NLP Analysis (NEW):
    ├─ Summarize (Bedrock Claude 3)
    ├─ Fraud Analysis (Bedrock + Comprehend)
    ├─ Entity Extraction (Comprehend + Bedrock)
    └─ Claim Type Classification (Bedrock)
→ Rules Engine (Enhanced with NLP data)
→ ML Scoring (Combines OCR + NLP signals)
→ Decision
→ AI-Generated Personalized Email
```

### Cost
- **Per claim**: $0.00048 (Claude 3 Haiku + Comprehend)
- **Monthly (1000 claims)**: $0.48

---

## Files to Work On

### CREATE (New Files)

#### 1. `src/Claims.Services/Aws/AWSBedrockService.cs`
**Purpose**: Wrapper for Claude 3 model invocation  
**Lines**: ~80  
**Dependencies**: AWSSDK.Bedrock, IConfiguration, ILogger

#### 2. Unit Tests (Optional but recommended)
**File**: `src/Claims.Services.Tests/Aws/AWSNlpServiceTests.cs`  
**Lines**: ~100  
**Purpose**: Validate NLP methods work correctly

### MODIFY (Existing Files)

#### 1. `src/Claims.Services/Claims.Services.csproj`
**Change**: Add NuGet packages
```xml
<ItemGroup>
    <PackageReference Include="AWSSDK.Bedrock" Version="3.7.100.37" />
    <PackageReference Include="AWSSDK.Comprehend" Version="3.7.400.5" />
</ItemGroup>
```

#### 2. `src/Claims.Services/Aws/AWSNlpService.cs`
**Change**: Replace entire file with enhanced implementation (Bedrock integration)  
**Lines**: ~400  
**Methods**:
- `SummarizeClaimAsync()` - Bedrock
- `AnalyzeFraudNarrativeAsync()` - Bedrock + Comprehend
- `ExtractEntitiesAsync()` - Comprehend + Bedrock
- `GenerateClaimResponseAsync()` - Bedrock

#### 3. `src/Claims.Api/appsettings.json`
**Change**: Add Bedrock configuration
```json
{
  "AWS": {
    "Enabled": true,
    "Region": "us-east-1",
    "AccessKey": "YOUR_KEY",
    "SecretKey": "YOUR_SECRET",
    "Bedrock": {
      "Enabled": true,
      "Model": "anthropic.claude-3-5-haiku-20241022-v1:0",
      "MaxTokens": 1024,
      "Temperature": 0.7
    }
  },
  "FeatureFlags": {
    "UseAWSBedrock": true
  }
}
```

#### 4. `src/Claims.Api/Program.cs`
**Change**: Update NLP service registration
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

#### 5. `src/Claims.Services/Implementations/ClaimsService.cs`
**Changes**:
- Add `private readonly INlpService _nlpService;` field
- Add `INlpService nlpService` parameter to constructor
- Add NLP processing step after document analysis
- Combine NLP fraud score with ML fraud score (60/40 weight)

**New code to add** (after Step 2: Document Analysis):
```csharp
// Step 2.5: NLP Analysis
_logger.LogInformation("Step 2.5: NLP Analysis");

var ocrText = ocrResults.FirstOrDefault()?.ExtractedText ?? string.Empty;
var summary = await _nlpService.SummarizeClaimAsync(
    claim.Description ?? string.Empty,
    ocrText);
claim.Description = summary;

var fraudNarrativeJson = await _nlpService.AnalyzeFraudNarrativeAsync(summary);
var fraudNarrative = JsonSerializer.Deserialize<JsonElement>(fraudNarrativeJson);
var narrativeFraudScore = (decimal)fraudNarrative.GetProperty("riskScore").GetDouble();

var entitiesJson = await _nlpService.ExtractEntitiesAsync(ocrText);

result.NlpAnalysis = new NlpAnalysisResult
{
    Summary = summary,
    FraudRiskScore = narrativeFraudScore,
    DetectedEntities = entitiesJson
};
```

**Modified ML Scoring step**:
```csharp
// Combine OCR and NLP fraud signals
var nlpScore = ExtractFraudScore(fraudNarrativeJson);
var combinedFraudScore = (fraudScore * 0.6m) + (nlpScore * 0.4m);
claim.FraudScore = combinedFraudScore;
```

#### 6. `src/Claims.Domain/DTOs/ClaimProcessingResult.cs`
**Change**: Add NLP result class
```csharp
public class NlpAnalysisResult
{
    public string? Summary { get; set; }
    public decimal FraudRiskScore { get; set; }
    public string? DetectedEntities { get; set; }
    public string? ClaimType { get; set; }
}

// In ClaimProcessingResult class add:
public NlpAnalysisResult? NlpAnalysis { get; set; }
```

---

## Implementation Steps

### STEP 1: Install NuGet Packages (10 min)

```powershell
cd C:\Demo_Projects\Claim_Validation

# Install Bedrock and Comprehend
dotnet add src/Claims.Services/Claims.Services.csproj package AWSSDK.Bedrock
dotnet add src/Claims.Services/Claims.Services.csproj package AWSSDK.Comprehend

# Restore
dotnet restore
```

**Verification**:
```powershell
dotnet list package | findstr /I "bedrock comprehend"
```

---

### STEP 2: Create AWSBedrockService (30 min)

**File**: `src/Claims.Services/Aws/AWSBedrockService.cs`

```csharp
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Claims.Services.Aws;

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

    public async Task<string> InvokeClaudeAsync(string prompt)
    {
        try
        {
            var requestBody = System.Text.Json.JsonSerializer.Serialize(new
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
            });

            var request = new InvokeModelRequest
            {
                ModelId = _modelId,
                Body = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(requestBody)),
                ContentType = "application/json"
            };

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

---

### STEP 3: Replace AWSNlpService (60 min)

**File**: `src/Claims.Services/Aws/AWSNlpService.cs`

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
            _logger.LogWarning("AWS NLP service is disabled");
        }
    }

    public bool IsEnabled => _isEnabled;

    public async Task<string> SummarizeClaimAsync(string claimDescription, string documentText)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("AWS NLP not enabled, returning original");
            return claimDescription;
        }

        try
        {
            var combined = $"{claimDescription}\n\nDocument:\n{documentText}";
            var truncated = combined.Substring(0, Math.Min(3000, combined.Length));

            var prompt = $@"Summarize this insurance claim in 2-3 sentences. Focus on what, when, where, and amount.

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

    public async Task<string> AnalyzeFraudNarrativeAsync(string claimDescription)
    {
        if (!IsEnabled)
        {
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

            var sentiment = await _comprehendClient.DetectSentimentAsync(new DetectSentimentRequest
            {
                Text = truncated,
                LanguageCode = "en"
            });

            var fraudPrompt = $@"Analyze this claim for fraud risk (0.0 to 1.0). Look for inconsistencies, vague details, unrealistic claims.

{truncated}

Return ONLY JSON:
{{
  ""riskScore"": <0.0-1.0>,
  ""riskLevel"": ""Low""|""Medium""|""High"",
  ""indicators"": [""indicator1""],
  ""recommendation"": ""Approve""|""Review""|""Investigate""
}}";

            var fraudAnalysis = await _bedrockService.InvokeClaudeAsync(fraudPrompt);
            
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
            catch
            {
                return fraudAnalysis;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing fraud");
            return JsonSerializer.Serialize(new
            {
                riskScore = 0.5m,
                riskLevel = "Medium",
                indicators = new[] { "Analysis failed" },
                recommendation = "Review"
            });
        }
    }

    public async Task<string> ExtractEntitiesAsync(string text)
    {
        if (!IsEnabled)
        {
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

            var entities = await _comprehendClient.DetectEntitiesAsync(new DetectEntitiesRequest
            {
                Text = truncated,
                LanguageCode = "en"
            });

            var names = entities.Entities.Where(e => e.Type == "PERSON").Select(e => e.Text).ToList();
            var dates = entities.Entities.Where(e => e.Type == "DATE").Select(e => e.Text).ToList();
            var locations = entities.Entities.Where(e => e.Type == "LOCATION").Select(e => e.Text).ToList();

            var classPrompt = $@"Extract claim type and amounts from:
{truncated}

Return JSON:
{{
  ""claimType"": ""medical""|""auto""|""property""|""life""|""other"",
  ""amounts"": [100.00, 200.00]
}}";

            var classification = await _bedrockService.InvokeClaudeAsync(classPrompt);
            
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

                _logger.LogInformation("Entities extracted: {Count} names, {DateCount} dates", 
                    names.Count, dates.Count);
                
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

    public async Task<string> GenerateClaimResponseAsync(string claimantName, string decision, string reason, decimal? amount)
    {
        if (!IsEnabled)
        {
            return GenerateTemplate(claimantName, decision, reason, amount);
        }

        try
        {
            var prompt = $@"Write a professional insurance claim decision letter.

Claimant: {claimantName}
Decision: {decision}
Reason: {reason}
Amount: {(amount.HasValue ? $"${amount.Value:F2}" : "N/A")}

Letter:";

            var response = await _bedrockService.InvokeClaudeAsync(prompt);
            _logger.LogInformation("Response generated for {ClaimantName}", claimantName);
            return response.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response");
            return GenerateTemplate(claimantName, decision, reason, amount);
        }
    }

    private static string GenerateTemplate(string claimantName, string decision, string reason, decimal? amount)
    {
        return $@"Dear {claimantName},

Your claim has been {decision.ToLower()}. {reason}

{(amount.HasValue ? $"Amount: ${amount.Value:F2}" : "")}

Best regards,
Claims Team";
    }
}
```

---

### STEP 4: Update Configuration (10 min)

**File**: `src/Claims.Api/appsettings.json`

Add to AWS section:
```json
{
  "AWS": {
    "Enabled": true,
    "Region": "us-east-1",
    "AccessKey": "YOUR_AWS_ACCESS_KEY",
    "SecretKey": "YOUR_AWS_SECRET_KEY",
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

---

### STEP 5: Update Program.cs (15 min)

**File**: `src/Claims.Api/Program.cs`

Find NLP service registration section and replace with:

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

---

### STEP 6: Update ClaimsService (45 min)

**File**: `src/Claims.Services/Implementations/ClaimsService.cs`

**6.1: Add field**
```csharp
private readonly INlpService _nlpService;
```

**6.2: Add constructor parameter**
```csharp
public ClaimsService(
    IClaimsService claimsService,
    IDocumentAnalysisService documentAnalysisService,
    IRulesEngineService rulesEngineService,
    IMlScoringService mlScoringService,
    INotificationService notificationService,
    INlpService nlpService)  // ADD THIS
{
    _claimsService = claimsService;
    _documentAnalysisService = documentAnalysisService;
    _rulesEngineService = rulesEngineService;
    _mlScoringService = mlScoringService;
    _notificationService = notificationService;
    _nlpService = nlpService;  // ADD THIS
}
```

**6.3: Add NLP processing in ProcessClaimAsync()**

After document analysis step, add:
```csharp
// Step 2.5: NLP Analysis
_logger.LogInformation("Step 2.5: NLP Analysis");

var ocrText = ocrResults.FirstOrDefault()?.ExtractedText ?? string.Empty;
var summary = await _nlpService.SummarizeClaimAsync(
    claim.Description ?? string.Empty,
    ocrText);
claim.Description = summary;

var fraudNarrativeJson = await _nlpService.AnalyzeFraudNarrativeAsync(summary);
var fraudNarrative = JsonSerializer.Deserialize<JsonElement>(fraudNarrativeJson);
var narrativeFraudScore = (decimal)fraudNarrative.GetProperty("riskScore").GetDouble();

var entitiesJson = await _nlpService.ExtractEntitiesAsync(ocrText);

result.NlpAnalysis = new NlpAnalysisResult
{
    Summary = summary,
    FraudRiskScore = narrativeFraudScore,
    DetectedEntities = entitiesJson
};
```

**6.4: Combine fraud scores in ML scoring step**

Replace existing fraud score assignment:
```csharp
var nlpScore = ExtractScoreFromJson(fraudNarrativeJson);
var combinedFraudScore = (fraudScore * 0.6m) + (nlpScore * 0.4m);
claim.FraudScore = combinedFraudScore;
```

---

### STEP 7: Update Domain Models (10 min)

**File**: `src/Claims.Domain/DTOs/ClaimProcessingResult.cs`

Add class:
```csharp
public class NlpAnalysisResult
{
    public string? Summary { get; set; }
    public decimal FraudRiskScore { get; set; }
    public string? DetectedEntities { get; set; }
    public string? ClaimType { get; set; }
}
```

Add to ClaimProcessingResult:
```csharp
public NlpAnalysisResult? NlpAnalysis { get; set; }
```

---

### STEP 8: Build & Test (90 min)

**8.1: Build Solution**
```powershell
dotnet build ClaimsValidation.sln
```
- [ ] All 4 projects compile
- [ ] No errors or warnings

**8.2: Create Unit Tests** (Optional)
```powershell
# Create test file: src/Claims.Services.Tests/Aws/AWSNlpServiceTests.cs
```

**8.3: Run Tests**
```powershell
dotnet test ClaimsValidation.sln
```

**8.4: Manual Testing**
```powershell
cd src/Claims.Api
dotnet run
```

Test via Swagger or Postman:
- POST /api/claims with test claim
- Verify NLP results in response
- Check logs for NLP processing steps
- Validate fraud scores in reasonable range (0.0-1.0)
- Confirm response generation works

---

## Prerequisites Checklist

Before starting:

- [ ] AWS Account with Bedrock & Comprehend access
- [ ] AWS Access Key & Secret Key ready
- [ ] Bedrock model access requested (Claude 3 Haiku)
- [ ] .NET 9.0 SDK installed
- [ ] Solution builds currently without errors
- [ ] VS Code or Visual Studio open
- [ ] Terminal access ready

---

## Success Criteria

✅ **Code**
- [ ] AWSBedrockService.cs created and compiles
- [ ] AWSNlpService.cs enhanced with Bedrock
- [ ] Program.cs NLP registration updated
- [ ] ClaimsService integrates NLP
- [ ] Domain models updated
- [ ] No compilation errors

✅ **Functional**
- [ ] Claims process with NLP analysis < 3 seconds
- [ ] Fraud scores between 0.0 and 1.0
- [ ] Entities correctly extracted
- [ ] Summaries are coherent
- [ ] Response letters are professional

✅ **Operational**
- [ ] Configuration via appsettings.json
- [ ] AWS credentials properly stored
- [ ] Logging shows NLP operations
- [ ] Cost tracking < $1/month for MVP

---

## Rollback Plan

If critical issues arise:

```powershell
# Revert NLP service
git checkout HEAD -- src/Claims.Services/Aws/AWSNlpService.cs

# Remove Bedrock service
rm src/Claims.Services/Aws/AWSBedrockService.cs

# Revert Program.cs
git checkout HEAD -- src/Claims.Api/Program.cs

# Revert ClaimsService
git checkout HEAD -- src/Claims.Services/Implementations/ClaimsService.cs

# Rebuild
dotnet build ClaimsValidation.sln
```

---

## Timeline

| Phase | Duration | Tasks |
|-------|----------|-------|
| Setup | 10 min | Install packages, verify |
| Build | 30 min | Create AWSBedrockService |
| Enhance | 60 min | Replace AWSNlpService |
| Config | 10 min | Update appsettings.json |
| Register | 15 min | Update Program.cs |
| Integrate | 45 min | Modify ClaimsService |
| Test | 90 min | Build, unit tests, manual tests |
| **Total** | **260 min** | **~4.3 hours** |

---

## References

- **AWS Documentation**: https://docs.aws.amazon.com/bedrock/
- **Claude API**: https://docs.anthropic.com/claude/
- **AWS Comprehend**: https://docs.aws.amazon.com/comprehend/
- **NuGet Packages**: 
  - AWSSDK.Bedrock: 3.7.100.37
  - AWSSDK.Comprehend: 3.7.400.5

---

## Next Action

→ Start **STEP 1: Install NuGet Packages**

