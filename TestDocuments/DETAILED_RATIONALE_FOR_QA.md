# 📋 NLP Integration - Detailed Rationale & QA Guide

**File-by-File, Method-by-Method Explanation with Sequential Flow**

---

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Component Breakdown](#component-breakdown)
3. [Sequential Flow Explanation](#sequential-flow-explanation)
4. [File-by-File Analysis](#file-by-file-analysis)
5. [Method-by-Method Details](#method-by-method-details)
6. [QA Testing Guide](#qa-testing-guide)
7. [Data Transformation Walkthrough](#data-transformation-walkthrough)

---

## Architecture Overview

### Why NLP Integration?

**Problem**: Traditional ML models for fraud detection rely only on structured claim features (amount, policy type, etc.).

**Solution**: Add AI-powered NLP analysis to examine the **narrative content** of claims:
- Read claim description/OCR text
- Analyze writing patterns for fraud indicators
- Extract key entities (names, dates, amounts)
- Combine with ML scoring for better accuracy

**Result**: **60% ML + 40% NLP = Better fraud detection**

---

## Component Breakdown

### High-Level Components

```
┌────────────────────────────────────────────────────────┐
│  API Layer (Claims.Api)                               │
│  • Controllers/ClaimsController.cs                     │
│  • appsettings.json (Configuration)                    │
│  • Program.cs (Dependency Injection)                   │
└────────────────────────┬─────────────────────────────┘
                         │
                         ▼
┌────────────────────────────────────────────────────────┐
│  Service Layer (Claims.Services)                       │
│  ├─ ClaimsService (Orchestration)                      │
│  ├─ AWSNlpService (NLP Processing) ⭐ NEW             │
│  └─ AWSBedrockService (Claude 3 API) ⭐ NEW           │
│                                                         │
│  Supporting Services:                                  │
│  ├─ DocumentAnalysisService (OCR)                      │
│  ├─ RulesEngineService (Business Rules)                │
│  ├─ MlScoringService (Traditional ML)                  │
│  └─ [Other services]                                   │
└────────────────────────┬─────────────────────────────┘
                         │
                         ▼
┌────────────────────────────────────────────────────────┐
│  Domain Layer (Claims.Domain)                          │
│  • DTOs/ClaimProcessingResult.cs (Response)            │
│  • Entities/Claim.cs (Data Model)                      │
└────────────────────────┬─────────────────────────────┘
                         │
                         ▼
┌────────────────────────────────────────────────────────┐
│  External Services                                      │
│  • AWS Bedrock (Claude 3 Haiku) - AI Analysis         │
│  • AWS Comprehend - Sentiment & Entities              │
│  • AWS S3 - Document Storage                           │
└────────────────────────────────────────────────────────┘
```

---

## Sequential Flow Explanation

### User Submits Claim: End-to-End Flow

```
STEP 0: USER INPUT
└─ HTTP POST /api/claims/submit-and-process
   {
     "policyId": "POL-2024-567890",
     "claimantId": "CLMT-JOHN-DAVIS",
     "totalAmount": 8850.00,
     "documents": [
       {
         "documentType": 0,
         "fileName": "claim.pdf",
         "base64Content": "JVBERi0xLjQK..."  ← Encoded PDF content
       }
     ]
   }

STEP 1: API RECEIVES REQUEST
└─ File: Claims.Api/Controllers/ClaimsController.cs
   Method: ProcessClaimAsync(...)
   Purpose: Validate HTTP request, deserialize JSON
   Action: Pass to ClaimsService

STEP 2: ORCHESTRATION BEGINS
└─ File: Claims.Services/Implementations/ClaimsService.cs
   Method: ProcessClaimAsync(claim)
   Purpose: Orchestrate entire claim processing pipeline
   Action: Call sub-services in sequence

STEP 2.1: OCR EXTRACTION
└─ File: Claims.Services/Implementations/DocumentAnalysisService.cs
   Method: ExtractTextAsync(documentPath)
   Purpose: Read document, extract text via OCR
   Input: Document file path or content
   Output: extractedText (string)
   Example Output: "CLAIM INFORMATION\nClaim Number: CLM-2024-001234..."

STEP 2.2: BUSINESS RULES VALIDATION
└─ File: Claims.Services/Implementations/RulesEngineService.cs
   Method: ValidateAsync(claim)
   Purpose: Check policy limits, validity, coverage
   Input: Claim entity
   Output: isValid (boolean), ruleResults (object)
   Example: Verifies claim amount < policy limit

STEP 2.3: ⭐ NLP ANALYSIS (NEW!)
└─ File: Claims.Services/Aws/AWSNlpService.cs
   Purpose: AI-powered claim analysis using Bedrock + Comprehend
   
   ├─ Method 1: SummarizeClaimAsync(claimAmount, ocrText)
   │  Purpose: Generate concise 2-3 sentence summary
   │  Input: "$8850.00" + full OCR extracted text
   │  Process:
   │    1. Create prompt: "Summarize this claim in 2-3 sentences..."
   │    2. Send prompt to AWSBedrockService
   │    3. Claude 3 Haiku processes and responds
   │  Output: "Auto accident claim for John Davis..."
   │  Why: Provides human-readable summary for review
   │
   ├─ Method 2: AnalyzeFraudNarrativeAsync(summary)
   │  Purpose: Calculate fraud risk from claim narrative
   │  Input: Summary from Method 1
   │  Process:
   │    1. Create fraud analysis prompt: "Is this claim fraudulent? Score 0-1..."
   │    2. Send to AWSBedrockService → Claude 3 analyzes
   │       Output: { "riskScore": 0.28, "reasoning": "..." }
   │    3. Send summary to AmazonComprehendClient → DetectSentimentAsync()
   │       Output: sentiment (POSITIVE/NEGATIVE/NEUTRAL), confidence scores
   │    4. Apply sentiment bonus:
   │       if (sentiment == NEGATIVE) fraudScore += 0.15
   │    5. Clamp to range [0.0, 1.0]
   │  Output: fraudRiskScore (0.28 in this example)
   │  Why: AI detects fraud patterns humans might miss
   │
   ├─ Method 3: ExtractEntitiesAsync(ocrText)
   │  Purpose: Extract key information (names, dates, amounts, locations)
   │  Input: Full OCR text from Step 2.1
   │  Process:
   │    1. Call AmazonComprehendClient.DetectEntitiesAsync()
   │       Comprehend identifies: PERSON, DATE, LOCATION, QUANTITY
   │       Output: [
   │         { "type": "PERSON", "text": "John Michael Davis" },
   │         { "type": "DATE", "text": "January 15, 2024" },
   │         { "type": "LOCATION", "text": "Springfield, IL" },
   │         { "type": "QUANTITY", "text": "$8,850" }
   │       ]
   │    2. Call AWSBedrockService to classify claim type
   │       Prompt: "What type of claim? auto/medical/property/life?"
   │       Output: { "claimType": "auto" }
   │    3. Combine into JSON response
   │  Output: { "names": [...], "dates": [...], "amounts": [...], "claimType": "auto" }
   │  Why: Structured data for audit trail and system processing
   │
   └─ Method 4: GenerateClaimResponseAsync(summary)
      Purpose: Generate professional response letter (optional)
      Input: Claim summary
      Output: HTML formatted response template
      Why: Template for claim decision communication

STEP 2.4: TRADITIONAL ML SCORING
└─ File: Claims.Services/Implementations/MlScoringService.cs
   Method: ScoreClaimAsync(claim)
   Purpose: Traditional machine learning fraud scoring
   Input: Claim features (amount, type, policy history)
   Process: ML model analyzes structured features
   Output: fraudScore_ML = 0.45 (example)
   Why: Baseline fraud detection from existing model

STEP 2.5: ⭐ FRAUD SCORE COMBINATION (NEW!)
└─ File: Claims.Services/Implementations/ClaimsService.cs
   Method: ProcessClaimAsync(...) [continuation]
   Purpose: Merge ML and NLP scores using weighted formula
   
   Input:
   ├─ fraudScore_ML = 0.45 (from Step 2.4)
   └─ fraudScore_NLP = 0.28 (from Step 2.3, Method 2)
   
   Formula:
   combinedFraudScore = (fraudScore_ML × 0.60) + (fraudScore_NLP × 0.40)
                      = (0.45 × 0.60) + (0.28 × 0.40)
                      = 0.27 + 0.112
                      = 0.382 (≈ 0.38)
   
   Rationale for 60/40 split:
   • ML (60%): More mature, proven track record
   • NLP (40%): New capability, complement ML weaknesses
   • Clamped to range [0.0, 1.0] for validity
   
   Output: combinedFraudScore = 0.38

STEP 2.6: FINAL DECISION
└─ File: Claims.Services/Implementations/ClaimsService.cs
   Method: ProcessClaimAsync(...) [continuation]
   Purpose: Route claim to AutoApprove, Reject, or ManualReview
   
   Decision Logic:
   ├─ if (fraudScore < 0.30 && approvalScore > 0.80)
   │  └─ Decision: "AutoApprove"
   │     Reason: Low risk, high approval confidence
   │
   ├─ else if (fraudScore > 0.70)
   │  └─ Decision: "Reject"
   │     Reason: High fraud risk, denial recommended
   │
   └─ else
      └─ Decision: "ManualReview"
         Reason: Moderate risk, requires human review

STEP 3: BUILD RESPONSE
└─ File: Claims.Domain/DTOs/ClaimProcessingResult.cs
   Purpose: Package all results into JSON response object
   
   Response Structure:
   {
     "claimId": "550e8400-...",           ← Unique identifier
     "success": true,                     ← Processing successful
     "finalDecision": "ManualReview",     ← Decision made
     "decisionReason": "Moderate risk...", ← Explanation
     
     "ocrResults": [                      ← From Step 2.1
       {
         "documentType": "AccidentReport",
         "extractedText": "CLAIM INFORMATION...",
         "confidence": 0.95
       }
     ],
     
     "rulesValidation": {                 ← From Step 2.2
       "isValid": true,
       "rulesChecked": ["PolicyLimit", "PolicyValidity"]
     },
     
     "nlpAnalysis": {                     ← From Step 2.3 ⭐ NEW
       "summary": "Auto accident claim...",
       "fraudRiskScore": 0.28,
       "detectedEntities": "{...}",
       "claimType": "auto"
     },
     
     "mlScoring": {                       ← From Step 2.4 & 2.5
       "fraudScore": 0.38,                ← Combined score!
       "approvalScore": 0.65,
       "fraudRiskLevel": "Low"
     },
     
     "processingTimeMs": 2847.5           ← Performance metric
   }

STEP 4: RETURN TO CLIENT
└─ HTTP 200 OK Response
   Send JSON response with all results
```

---

## File-by-File Analysis

### 1. AWSBedrockService.cs (NEW)

**Location**: `src/Claims.Services/Aws/AWSBedrockService.cs`

**Purpose**: Wrapper around AWS Bedrock API for Claude 3 model

**Why Created**:
- Centralized Bedrock integration
- Reusable across all NLP services
- Easy to mock for testing
- Clean separation of concerns

**Constructor**:
```
AWSBedrockService(IConfiguration, ILogger)
├─ Reads config from appsettings.json
│  ├─ AWS:AccessKey
│  ├─ AWS:SecretKey
│  ├─ AWS:Region
│  ├─ AWS:Bedrock:Model
│  ├─ AWS:Bedrock:MaxTokens
│  └─ AWS:Bedrock:Temperature
├─ Creates AWS credentials
├─ Initializes AmazonBedrockRuntimeClient
└─ Stores model parameters for reuse
```

**Key Method**: `InvokeClaudeAsync(string prompt)`

```
What it does:
1. Takes user prompt (e.g., "Summarize this claim...")
2. Wraps prompt in JSON message format:
   {
     "anthropic_version": "bedrock-2023-06-01",
     "max_tokens": 1024,
     "temperature": 0.7,
     "messages": [
       {
         "role": "user",
         "content": "YOUR PROMPT HERE"
       }
     ]
   }
3. Sends to AWS Bedrock Runtime API
4. Receives response JSON:
   {
     "content": [
       {
         "type": "text",
         "text": "Claude 3's response here..."
       }
     ]
   }
5. Extracts text from response
6. Returns string to caller

Error Handling:
- Logs errors with context
- Re-throws for caller to handle
- Allows graceful fallback
```

**QA Testing for AWSBedrockService**:
- ✅ Verify AWS credentials in appsettings.json are valid
- ✅ Verify API calls to Bedrock succeed (check CloudWatch logs)
- ✅ Verify responses are properly parsed
- ✅ Verify errors are logged with detail

---

### 2. AWSNlpService.cs (MODIFIED - Complete Rewrite)

**Location**: `src/Claims.Services/Aws/AWSNlpService.cs`

**Purpose**: Implements INlpService interface with Bedrock + Comprehend integration

**Why Rewritten**:
- Previous version was placeholder/incomplete
- Now provides complete NLP pipeline
- Uses Bedrock for AI analysis
- Uses Comprehend for NLP operations

**Implementation of INlpService**:

```csharp
public interface INlpService
{
    Task<string> SummarizeClaimAsync(string claimAmount, string ocrText);
    Task<string> AnalyzeFraudNarrativeAsync(string claimSummary);
    Task<string> ExtractEntitiesAsync(string ocrText);
    Task<string> GenerateClaimResponseAsync(string claimSummary);
}
```

**Method 1: SummarizeClaimAsync(claimAmount, ocrText)**

```
Input:
├─ claimAmount: "8850.00"
└─ ocrText: Full text from OCR extraction

Process:
1. Build prompt:
   "Summarize this insurance claim in 2-3 sentences. 
    Claim Amount: $8850.00
    Claim Details: [OCR TEXT HERE]"

2. Call AWSBedrockService.InvokeClaudeAsync(prompt)
   └─ Claude 3 Haiku processes prompt
   └─ Returns concise summary

3. Log result for audit trail

Output:
"Auto accident claim for John Davis. Vehicle collision at 
Main and Oak intersection on Jan 15, 2024. Total damages 
$8,850 after deductible. Two witnesses present."

Why:
- Provides quick overview without reading full document
- Used as input for fraud analysis
- Human-readable for review
```

**Method 2: AnalyzeFraudNarrativeAsync(claimSummary)**

```
Input:
└─ claimSummary: Output from Method 1 (2-3 sentences)

Process (Two-Step):

Step A: Bedrock Fraud Analysis
├─ Build prompt:
│  "Analyze this claim summary for fraud indicators.
│   Rate fraud risk as decimal 0.0 (safe) to 1.0 (fraud).
│   Return JSON: { \"riskScore\": X.XX, \"reasoning\": \"...\" }
│   Summary: [CLAIM SUMMARY HERE]"
│
├─ Call AWSBedrockService.InvokeClaudeAsync(prompt)
│  └─ Claude 3 analyzes narrative patterns
│  └─ Returns: { "riskScore": 0.28, "reasoning": "..." }
│
└─ Parse JSON response
   └─ fraudRiskScore_bedrock = 0.28

Step B: Comprehend Sentiment Analysis
├─ Call AmazonComprehendClient.DetectSentimentAsync(summary)
│  └─ Comprehend analyzes emotional tone
│  └─ Returns: { 
│       "sentiment": "NEUTRAL",
│       "sentimentScore": { 
│         "positive": 0.1, 
│         "negative": 0.05, 
│         "neutral": 0.85 
│       }
│     }
│
└─ Apply fraud bonus:
   if (sentiment == "NEGATIVE")
     fraudRiskScore += 0.15  ← Negative language suspicious
   else if (sentiment == "POSITIVE")
     fraudRiskScore -= 0.05  ← Positive language less suspicious

Step C: Clamp to valid range
└─ fraudRiskScore = Math.Max(0.0, Math.Min(fraudRiskScore, 1.0))

Output:
{ 
  "riskScore": 0.28,
  "sentiment": "NEUTRAL",
  "reasoning": "Claim narrative is consistent..."
}

Why:
- Bedrock detects fraud patterns (contradictions, exaggeration)
- Sentiment analysis detects emotional red flags
- Combined approach more accurate than either alone
```

**Method 3: ExtractEntitiesAsync(ocrText)**

```
Input:
└─ ocrText: Full OCR text from document

Process (Two-Step):

Step A: Comprehend Entity Detection
├─ Call AmazonComprehendClient.DetectEntitiesAsync(ocrText)
│  └─ Comprehend identifies named entities
│  └─ Returns: [
│       { "type": "PERSON", "text": "John Michael Davis", "score": 0.98 },
│       { "type": "DATE", "text": "January 15, 2024", "score": 0.99 },
│       { "type": "LOCATION", "text": "Springfield, IL", "score": 0.95 },
│       { "type": "QUANTITY", "text": "$8,850", "score": 0.97 }
│     ]
│
└─ Group entities by type:
   └─ names: ["John Michael Davis", "Robert James Thompson"]
   └─ dates: ["January 15, 2024", "January 20, 2024"]
   └─ locations: ["Springfield, IL"]
   └─ amounts: [8850.00, 8500.00, 450.00]

Step B: Bedrock Claim Type Classification
├─ Build prompt:
│  "What type of insurance claim is this?
│   Options: auto, medical, property, life, workers_comp
│   Return: { \"claimType\": \"[type]\" }
│   Document: [OCR TEXT]"
│
├─ Call AWSBedrockService.InvokeClaudeAsync(prompt)
│  └─ Claude 3 classifies based on content
│  └─ Returns: { "claimType": "auto" }
│
└─ Parse response

Step C: Combine Results
└─ Return JSON: {
     "names": ["John Michael Davis", "Robert James Thompson"],
     "dates": ["January 15, 2024", "January 20, 2024"],
     "locations": ["Springfield, IL"],
     "amounts": [8850.00, 8500.00, 450.00],
     "claimType": "auto"
   }

Output Format:
JSON string with all entities organized by type

Why:
- Provides structured data for audit/compliance
- Verifies claim amounts are consistent
- Identifies all involved parties
- Classifies claim type automatically
- Used for fraud pattern detection
```

**Method 4: GenerateClaimResponseAsync(claimSummary)**

```
Input:
└─ claimSummary: 2-3 sentence summary from Method 1

Process:
1. Build prompt:
   "Generate a professional insurance response letter 
    for this claim decision in HTML format.
    Claim Summary: [SUMMARY HERE]"

2. Call AWSBedrockService.InvokeClaudeAsync(prompt)
   └─ Claude 3 generates response template

3. Return HTML string

Output:
"<html>
  <body>
    <h1>Claim Decision Letter</h1>
    <p>Dear Claimant...</p>
  </body>
</html>"

Why:
- Automates response letter generation
- Ensures consistent professional format
- Reduces manual work for claims team
```

**QA Testing for AWSNlpService**:
- ✅ Verify SummarizeClaimAsync returns 2-3 sentences
- ✅ Verify AnalyzeFraudNarrativeAsync returns valid fraud score (0.0-1.0)
- ✅ Verify ExtractEntitiesAsync identifies all names, dates, amounts
- ✅ Verify sentiment bonus is applied correctly
- ✅ Verify claim type classification is accurate
- ✅ Verify all JSON responses are valid and properly formatted

---

### 3. ClaimsService.cs (MODIFIED)

**Location**: `src/Claims.Services/Implementations/ClaimsService.cs`

**What Changed**: Added NLP integration into processing pipeline

**Constructor Change**:

```
Before:
public ClaimsService(
    IClaimsRepository claimsRepository,
    IDocumentAnalysisService documentAnalysisService,
    IRulesEngineService rulesEngineService,
    IMlScoringService mlScoringService,
    INotificationService notificationService)

After (ADDED):
public ClaimsService(
    IClaimsRepository claimsRepository,
    IDocumentAnalysisService documentAnalysisService,
    IRulesEngineService rulesEngineService,
    INlpService nlpService,                    ← NEW!
    IMlScoringService mlScoringService,
    INotificationService notificationService)

Injection:
private readonly INlpService _nlpService;     ← NEW!
```

**ProcessClaimAsync Method - Key Changes**:

```
// Step 2.1: OCR Processing (unchanged)
var ocrResults = await _documentAnalysisService.ExtractTextAsync(documentPath);
var ocrText = ocrResults.First().ExtractedText;

// Step 2.2: Rules Validation (unchanged)
var rulesResult = await _rulesEngineService.ValidateAsync(claim);

// NEW! Step 2.3: NLP Analysis
// ════════════════════════════════════════════════════════════
// Step 2.3a: Summarize Claim
var summary = await _nlpService.SummarizeClaimAsync(
    claim.TotalAmount.ToString(),
    ocrText);

// Step 2.3b: Analyze Fraud Narrative
var fraudNarrativeJson = await _nlpService.AnalyzeFraudNarrativeAsync(summary);
var narrativeFraudScore = (decimal)JsonDocument
    .Parse(fraudNarrativeJson)
    .RootElement
    .GetProperty("riskScore")
    .GetDouble();

// Step 2.3c: Extract Entities
var entitiesJson = await _nlpService.ExtractEntitiesAsync(ocrText);
var entities = JsonDocument.Parse(entitiesJson);
var claimType = entities.RootElement.GetProperty("claimType").GetString();

// Step 2.3d: Store NLP Results
result.NlpAnalysis = new NlpAnalysisResult
{
    Summary = summary,
    FraudRiskScore = narrativeFraudScore,
    DetectedEntities = entitiesJson,
    ClaimType = claimType
};
// ════════════════════════════════════════════════════════════

// Step 2.4: ML Scoring (unchanged)
decimal fraudScore_ML = await _mlScoringService.ScoreClaimAsync(claim);

// NEW! Step 2.5: Fraud Score Combination
// ════════════════════════════════════════════════════════════
// Before: claim.FraudScore = fraudScore_ML;
// After:
var combinedFraudScore = (fraudScore_ML * 0.6m) + (narrativeFraudScore * 0.4m);
combinedFraudScore = Math.Min(Math.Max(combinedFraudScore, 0.0m), 1.0m);
claim.FraudScore = combinedFraudScore;
// ════════════════════════════════════════════════════════════

// Step 2.6: Final Decision (updated to use combined score)
if (combinedFraudScore < 0.30m && approvalScore > 0.80m)
{
    claim.Status = "AutoApproved";
    result.FinalDecision = "AutoApprove";
}
else if (combinedFraudScore > 0.70m)
{
    claim.Status = "Rejected";
    result.FinalDecision = "Reject";
}
else
{
    claim.Status = "PendingManualReview";
    result.FinalDecision = "ManualReview";
}
```

**QA Testing for ClaimsService**:
- ✅ Verify NLP service is called (check logs for "Step 2.5" message)
- ✅ Verify combined fraud score is calculated correctly
- ✅ Verify formula: (ML × 0.6) + (NLP × 0.4)
- ✅ Verify fraud score is between 0.0-1.0
- ✅ Verify decision logic uses combined score
- ✅ Verify NlpAnalysis is populated in response
- ✅ Verify processing time includes NLP calls (~2-3 seconds longer)

---

### 4. Program.cs (MODIFIED)

**Location**: `src/Claims.Api/Program.cs`

**Purpose**: Dependency injection configuration

**Change**:

```csharp
// Added: NLP Service Registration
var useAwsBedrock = builder.Configuration.GetValue<bool>("AWS:Bedrock:Enabled");
var awsEnabled = builder.Configuration.GetValue<bool>("AWS:Enabled");

if (awsEnabled && useAwsBedrock)
{
    // Register Bedrock services (PREFERRED)
    builder.Services.AddSingleton<AWSBedrockService>();
    builder.Services.AddSingleton<INlpService, AWSNlpService>();
}
else
{
    // Fallback to default (if Bedrock disabled)
    builder.Services.AddSingleton<INlpService, DefaultNlpService>();
}
```

**Why**:
- Enables/disables NLP based on configuration
- Allows testing without AWS credentials
- Follows Dependency Injection pattern
- Allows swapping implementations

**QA Testing for Program.cs**:
- ✅ Verify AWSBedrockService is registered when AWS:Enabled=true
- ✅ Verify AWSNlpService is registered when Bedrock:Enabled=true
- ✅ Verify fallback works when AWS disabled
- ✅ Verify no exceptions during startup

---

### 5. appsettings.json (MODIFIED)

**Location**: `src/Claims.Api/appsettings.json`

**Added Section**:

```json
{
  "AWS": {
    "Enabled": true,
    "Region": "us-east-1",
    "AccessKey": "AKIA...",              ← Your AWS Access Key
    "SecretKey": "wJalr...",             ← Your AWS Secret Key
    "Bedrock": {
      "Enabled": true,
      "Model": "anthropic.claude-3-5-haiku-20241022-v1:0",
      "MaxTokens": 1024,
      "Temperature": 0.7
    }
  }
}
```

**Field Explanations**:

| Field | Value | Purpose |
|-------|-------|---------|
| Enabled | true/false | Turn AWS integration on/off |
| Region | us-east-1 | AWS region (must have Bedrock) |
| AccessKey | AKIA... | AWS IAM access key |
| SecretKey | wJalr... | AWS IAM secret key |
| Bedrock.Enabled | true/false | Turn Bedrock specifically on/off |
| Model | claude-3-5-haiku-20241022-v1:0 | Which Claude model to use |
| MaxTokens | 1024 | Max response length |
| Temperature | 0.7 | Creativity level (0.0=deterministic, 1.0=creative) |

**QA Testing for appsettings.json**:
- ✅ Verify AWS:Enabled is true
- ✅ Verify AWS credentials are valid (can auth to AWS)
- ✅ Verify Region is us-east-1 (has Bedrock)
- ✅ Verify Bedrock:Enabled is true
- ✅ Verify Model ID is correct
- ✅ Verify MaxTokens is reasonable (1024 is good)
- ✅ Verify Temperature is between 0.0 and 1.0

---

### 6. ClaimProcessingResult.cs (MODIFIED)

**Location**: `src/Claims.Domain/DTOs/ClaimProcessingResult.cs`

**Change**: Added NlpAnalysisResult class

```csharp
// NEW! Class
public class NlpAnalysisResult
{
    /// <summary>
    /// 2-3 sentence summary of the claim generated by Claude 3
    /// </summary>
    public string? Summary { get; set; }
    
    /// <summary>
    /// Fraud risk score from NLP analysis (0.0 = safe, 1.0 = fraud)
    /// </summary>
    public decimal FraudRiskScore { get; set; }
    
    /// <summary>
    /// JSON string containing detected entities:
    /// { "names": [...], "dates": [...], "locations": [...], "amounts": [...] }
    /// </summary>
    public string? DetectedEntities { get; set; }
    
    /// <summary>
    /// Claim type classification: auto, medical, property, life
    /// </summary>
    public string? ClaimType { get; set; }
}

// Updated ClaimProcessingResult
public class ClaimProcessingResult
{
    // ... existing properties ...
    
    /// <summary>
    /// NLP analysis results (NEW!)
    /// </summary>
    public NlpAnalysisResult? NlpAnalysis { get; set; }
}
```

**Example JSON Output**:

```json
{
  "nlpAnalysis": {
    "summary": "Auto accident claim for John Davis. Vehicle collision at Main and Oak intersection on Jan 15, 2024. Total damages $8,850 after deductible. Two witnesses present.",
    "fraudRiskScore": 0.28,
    "detectedEntities": "{\"names\":[\"John Michael Davis\",\"Robert James Thompson\"],\"dates\":[\"January 15, 2024\"],\"locations\":[\"Springfield, IL\"],\"amounts\":[8850.00]}",
    "claimType": "auto"
  }
}
```

**QA Testing for ClaimProcessingResult**:
- ✅ Verify nlpAnalysis is present in response
- ✅ Verify summary is not null and is readable
- ✅ Verify fraudRiskScore is decimal between 0.0-1.0
- ✅ Verify detectedEntities is valid JSON
- ✅ Verify claimType is one of valid values
- ✅ Verify JSON serialization works correctly

---

### 7. Claims.Services.csproj (MODIFIED)

**Location**: `src/Claims.Services/Claims.Services.csproj`

**Added Packages**:

```xml
<ItemGroup>
    <PackageReference Include="AWSSDK.Comprehend" Version="3.7.3" />
    <PackageReference Include="AWSSDK.BedrockRuntime" Version="3.7.200" />
</ItemGroup>
```

**Why Each Package**:

| Package | Version | Purpose | Used In |
|---------|---------|---------|---------|
| AWSSDK.BedrockRuntime | 3.7.200 | Invoke Claude 3 models | AWSBedrockService |
| AWSSDK.Comprehend | 3.7.3 | Entity extraction, sentiment | AWSNlpService |
| AWSSDK.Core | 3.7.100 | Base AWS SDK | All AWS services |

**QA Testing for Packages**:
- ✅ Verify packages restore successfully (`dotnet restore`)
- ✅ Verify no version conflicts
- ✅ Verify solution builds without errors
- ✅ Verify namespaces are available (Amazon.BedrockRuntime, Amazon.Comprehend)

---

## Method-by-Method Details

### Summary of All Methods

| File | Class | Method | Input | Output | Purpose |
|------|-------|--------|-------|--------|---------|
| AWSBedrockService.cs | AWSBedrockService | InvokeClaudeAsync | prompt | response text | Send prompt to Claude 3 |
| AWSNlpService.cs | AWSNlpService | SummarizeClaimAsync | amount, text | summary | Generate 2-3 sentence summary |
| AWSNlpService.cs | AWSNlpService | AnalyzeFraudNarrativeAsync | summary | fraud JSON | Calculate fraud risk from narrative |
| AWSNlpService.cs | AWSNlpService | ExtractEntitiesAsync | text | entities JSON | Extract names, dates, amounts |
| AWSNlpService.cs | AWSNlpService | GenerateClaimResponseAsync | summary | response HTML | Generate response letter |
| ClaimsService.cs | ClaimsService | ProcessClaimAsync | claimId | result object | Orchestrate entire pipeline |

---

## QA Testing Guide

### Test Scenario 1: Happy Path (Valid Claim)

**Test Case**: Submit legitimate auto accident claim

**Preconditions**:
- API is running
- AWS credentials are valid
- Bedrock is enabled in config
- Sample document exists

**Steps**:

1. **Start API**
   ```powershell
   cd src/Claims.Api
   dotnet run
   # Wait for: "Now listening on: http://localhost:5000"
   ```

2. **Submit Claim with Document**
   ```
   POST /api/claims/submit-and-process
   Body:
   {
     "policyId": "POL-2024-567890",
     "claimantId": "CLMT-JOHN-DAVIS",
     "totalAmount": 8850.00,
     "documents": [
       {
         "documentType": 0,
         "fileName": "claim.pdf",
         "base64Content": "JVBERi0xLjQK..."
       }
     ]
   }
   ```

3. **Verify Response**
   ```
   Check response includes:
   ✅ claimId (UUID)
   ✅ success = true
   ✅ nlpAnalysis object exists
   ✅ nlpAnalysis.summary is readable (2-3 sentences)
   ✅ nlpAnalysis.fraudRiskScore = 0.28 (between 0.0-1.0)
   ✅ nlpAnalysis.claimType = "auto"
   ✅ mlScoring.fraudScore = 0.38 (combined)
   ✅ finalDecision = "ManualReview"
   ✅ processingTimeMs ≈ 2800-4000 ms (includes NLP calls)
   ```

4. **Check Logs**
   ```
   Verify API console output contains:
   ✅ "Starting AI processing for claim..."
   ✅ "Step 1: OCR Processing..."
   ✅ "Step 2: Business Rules Validation..."
   ✅ "Step 2.5: NLP Analysis"          ← KEY INDICATOR
   ✅ "Claim summarized successfully"
   ✅ "Fraud analysis completed..."
   ✅ "Entities extracted..."
   ✅ "Step 3: ML Fraud Detection..."
   ✅ "Claim scored: FraudScore=..."
   ```

5. **Verify NLP Components**
   ```
   In response.nlpAnalysis:
   
   ✅ Summary Check
      • 2-3 sentences
      • Mentions claimant name (John Davis)
      • Mentions incident type (auto accident)
      • Mentions date (Jan 15, 2024)
   
   ✅ Fraud Risk Score Check
      • Number between 0.0 and 1.0
      • For this claim: should be ≤ 0.4 (legitimate)
   
   ✅ Detected Entities Check
      • Contains JSON with names array
      • Contains dates array
      • Contains locations array
      • Contains amounts array
   
   ✅ Claim Type Check
      • Value = "auto"
      • Matches document content
   ```

6. **Verify Fraud Score Combination**
   ```
   Calculate expected combined score:
   
   If ML Score = 0.45 and NLP Score = 0.28
   Expected Combined = (0.45 × 0.60) + (0.28 × 0.40)
                     = 0.27 + 0.112
                     = 0.382
   
   ✅ Response.mlScoring.fraudScore should equal ~0.38
   ```

---

### Test Scenario 2: Error Handling

**Test Case**: Process claim with missing AWS credentials

**Steps**:

1. **Set AWS:Enabled = false** in appsettings.json
2. **Restart API**
3. **Submit claim**
4. **Verify**:
   - ✅ API returns error or falls back
   - ✅ NlpAnalysis is null/empty
   - ✅ Error is logged with detail
   - ✅ API doesn't crash

---

### Test Scenario 3: Performance

**Test Case**: Measure NLP processing time

**Expected Results**:
- Bedrock summarization: 300-600 ms
- Fraud analysis (Bedrock + Comprehend): 600-1000 ms
- Entity extraction: 300-500 ms
- **Total NLP time**: 1200-2100 ms
- **Total request time**: 3000-4500 ms (with DB/other operations)

**QA Validation**:
```
✅ processingTimeMs < 5000
✅ NLP contributes 1-2 seconds
✅ Consistent across multiple runs
✅ No timeout errors
```

---

### Test Scenario 4: Different Claim Types

**Test with Medical Claim**:

```json
{
  "policyId": "POL-MED-123456",
  "claimantId": "CLMT-JANE-DOE",
  "totalAmount": 5000.00,
  "documents": [{
    "documentType": 0,
    "fileName": "medical.pdf",
    "base64Content": "..."
  }]
}
```

**Expected**:
- ✅ NlpAnalysis.claimType = "medical"
- ✅ Summary mentions medical terms (diagnosis, treatment, etc.)
- ✅ FraudRiskScore reflects medical fraud patterns

---

### Test Scenario 5: Fraud Detection

**Test with Suspicious Claim**:

```json
{
  "policyId": "POL-2024-999999",
  "claimantId": "CLMT-SUSPICIOUS",
  "totalAmount": 50000.00,
  "documents": [{
    "documentType": 0,
    "fileName": "suspicious.pdf",
    "base64Content": "..."
  }]
}
```

**Expected**:
- ✅ nlpAnalysis.fraudRiskScore > 0.6
- ✅ mlScoring.fraudScore > 0.6
- ✅ finalDecision = "Reject"
- ✅ Summary should note suspicious indicators

---

## Data Transformation Walkthrough

### Complete Data Journey

```
STEP 1: USER SUBMITS
┌────────────────────────────────────────┐
│ HTTP POST /api/claims/submit-and-process│
│ {                                       │
│   "policyId": "POL-2024-567890",      │
│   "claimantId": "CLMT-JOHN-DAVIS",    │
│   "totalAmount": 8850.00,              │
│   "documents": [{                       │
│     "documentType": 0,                  │
│     "fileName": "claim.pdf",           │
│     "base64Content": "JVBERi0xLjQK..."│
│   }]                                    │
│ }                                       │
└────────────────┬───────────────────────┘
                 │
                 ▼
STEP 2: CONTROLLER RECEIVES
┌────────────────────────────────────────┐
│ ClaimsController.ProcessClaimAsync()   │
│                                         │
│ Deserializes JSON                       │
│ Validates input                         │
│ Decodes base64 to binary document       │
└────────────────┬───────────────────────┘
                 │
                 ▼
STEP 3: CREATE CLAIM ENTITY
┌────────────────────────────────────────┐
│ Claim Entity                            │
│ {                                       │
│   ClaimId: 550e8400-...,               │
│   PolicyId: "POL-2024-567890",         │
│   ClaimantId: "CLMT-JOHN-DAVIS",       │
│   TotalAmount: 8850.00,                │
│   Status: "Processing",                │
│   SubmittedDate: 2026-01-02T10:00:00  │
│ }                                       │
│                                         │
│ Saved to Database                       │
└────────────────┬───────────────────────┘
                 │
                 ▼
STEP 4: EXTRACT OCR TEXT
┌────────────────────────────────────────┐
│ DocumentAnalysisService.ExtractTextAsync│
│                                         │
│ Input: Document binary (PDF)            │
│ Process: OCR extraction                 │
│ Output: Text string                     │
│                                         │
│ "CLAIM INFORMATION                      │
│  Claim Number: CLM-2024-001234          │
│  Date of Loss: January 15, 2024         │
│  Claim Type: Auto Accident              │
│  POLICYHOLDER INFORMATION               │
│  Name: John Michael Davis               │
│  Policy Number: POL-2024-567890         │
│  INCIDENT DETAILS                       │
│  Location: Main Street and Oak Avenue   │
│  ...                                    │
│  NET CLAIM REQUEST: $8,850.00           │
│ "                                       │
└────────────────┬───────────────────────┘
                 │
                 ▼
STEP 5: VALIDATE BUSINESS RULES
┌────────────────────────────────────────┐
│ RulesEngineService.ValidateAsync()     │
│                                         │
│ Checks:                                 │
│ • PolicyLimit: 8850 < 100000 ✓          │
│ • PolicyValidity: Not expired ✓         │
│ • CoverageCheck: Auto covered ✓         │
│                                         │
│ Output: { isValid: true }               │
└────────────────┬───────────────────────┘
                 │
                 ▼
STEP 6: ⭐ NLP ANALYSIS BEGINS
┌────────────────────────────────────────┐
│ AWSNlpService.SummarizeClaimAsync()    │
│                                         │
│ Input:                                  │
│  • claimAmount: "8850.00"              │
│  • ocrText: Full 126-line document     │
│                                         │
│ Prompt:                                 │
│  "Summarize this claim in 2-3 sentences│
│   Claim Amount: $8850.00                │
│   Details: [OCR TEXT]"                 │
│                                         │
│ Calls: AWSBedrockService.InvokeClaudeAsync│
│   → AWS Bedrock API                     │
│   → Claude 3 Haiku Model               │
│                                         │
│ Output:                                 │
│  "Auto accident claim for John Davis... │
│   Vehicle collision Jan 15, 2024...     │
│   Two witnesses present."               │
└────────────────┬───────────────────────┘
                 │
                 ▼
STEP 7: FRAUD NARRATIVE ANALYSIS
┌────────────────────────────────────────┐
│ AWSNlpService.AnalyzeFraudNarrativeAsync│
│                                         │
│ Input: Summary from Step 6              │
│                                         │
│ Part A: Bedrock Fraud Analysis          │
│  Prompt: "Rate fraud risk 0.0-1.0..."  │
│  Response: { "riskScore": 0.28 }       │
│                                         │
│ Part B: Comprehend Sentiment Analysis   │
│  Input: Summary text                    │
│  Response: { "sentiment": "NEUTRAL" }   │
│  No bonus applied (neutral = normal)    │
│                                         │
│ Output: fraudRiskScore = 0.28           │
└────────────────┬───────────────────────┘
                 │
                 ▼
STEP 8: ENTITY EXTRACTION
┌────────────────────────────────────────┐
│ AWSNlpService.ExtractEntitiesAsync()   │
│                                         │
│ Part A: Comprehend Entity Detection     │
│  Input: Full OCR text                   │
│  Extracts: Names, Dates, Locations     │
│  Output: [                              │
│    {PERSON: "John Michael Davis"},      │
│    {PERSON: "Robert James Thompson"},   │
│    {DATE: "January 15, 2024"},          │
│    {LOCATION: "Springfield, IL"},       │
│    {QUANTITY: "$8,850"}                 │
│  ]                                      │
│                                         │
│ Part B: Bedrock Claim Classification    │
│  Prompt: "What claim type?"             │
│  Response: { "claimType": "auto" }      │
│                                         │
│ Output: JSON with all entities          │
└────────────────┬───────────────────────┘
                 │
                 ▼
STEP 9: TRADITIONAL ML SCORING
┌────────────────────────────────────────┐
│ MlScoringService.ScoreClaimAsync()     │
│                                         │
│ Input: Claim features                   │
│  • Amount: 8850 (medium)                │
│  • Type: Auto (common)                  │
│  • History: Clean (good)                │
│                                         │
│ ML Model Analysis                       │
│  Fraud Pattern Detection                │
│  Feature Engineering                    │
│                                         │
│ Output:                                 │
│  • fraudScore_ML: 0.45                  │
│  • approvalScore: 0.65                  │
└────────────────┬───────────────────────┘
                 │
                 ▼
STEP 10: ⭐ FRAUD SCORE COMBINATION
┌────────────────────────────────────────┐
│ ClaimsService.ProcessClaimAsync()      │
│                                         │
│ Formula:                                │
│  combined = (ML × 0.60) + (NLP × 0.40) │
│  combined = (0.45 × 0.60) + (0.28 × 0.40)│
│  combined = 0.27 + 0.112               │
│  combined = 0.382 ≈ 0.38               │
│                                         │
│ Stored in: Claim.FraudScore = 0.38      │
└────────────────┬───────────────────────┘
                 │
                 ▼
STEP 11: FINAL DECISION
┌────────────────────────────────────────┐
│ Decision Logic                          │
│                                         │
│ if (fraudScore < 0.30 && approval > 0.80)│
│   → "AutoApprove" ❌ (fraud=0.38)      │
│                                         │
│ else if (fraudScore > 0.70)             │
│   → "Reject" ❌ (fraud=0.38)           │
│                                         │
│ else                                    │
│   → "ManualReview" ✅ (fraud=0.38)     │
│                                         │
│ Status: PendingManualReview             │
└────────────────┬───────────────────────┘
                 │
                 ▼
STEP 12: BUILD RESPONSE
┌────────────────────────────────────────┐
│ ClaimProcessingResult                   │
│ {                                       │
│   claimId: "550e8400-...",             │
│   success: true,                        │
│   finalDecision: "ManualReview",       │
│   decisionReason: "Moderate risk...",   │
│                                         │
│   ocrResults: [                         │
│     {                                   │
│       documentType: "AccidentReport",   │
│       extractedText: "CLAIM INFO...",  │
│       confidence: 0.95                  │
│     }                                   │
│   ],                                    │
│                                         │
│   rulesValidation: {                    │
│     isValid: true,                      │
│     ruleResults: [...]                  │
│   },                                    │
│                                         │
│   nlpAnalysis: { ⭐ NEW!               │
│     summary: "Auto accident claim...",  │
│     fraudRiskScore: 0.28,              │
│     detectedEntities: "{...}",         │
│     claimType: "auto"                   │
│   },                                    │
│                                         │
│   mlScoring: {                          │
│     fraudScore: 0.38,    ← COMBINED!   │
│     approvalScore: 0.65,               │
│     fraudRiskLevel: "Low"               │
│   },                                    │
│                                         │
│   processingTimeMs: 2847.5              │
│ }                                       │
└────────────────┬───────────────────────┘
                 │
                 ▼
STEP 13: RETURN RESPONSE
┌────────────────────────────────────────┐
│ HTTP 200 OK                             │
│ Content-Type: application/json          │
│ Body: ClaimProcessingResult JSON        │
│                                         │
│ Client receives complete results       │
└────────────────────────────────────────┘
```

---

## QA Checklist

### Pre-Testing Checklist

- [ ] Clone latest code
- [ ] Build solution: `dotnet build`
- [ ] Verify no compilation errors
- [ ] Check AWS credentials in appsettings.json
- [ ] Verify AWS:Bedrock:Enabled = true
- [ ] Verify AWS region = us-east-1
- [ ] Verify sample document exists

### During Testing Checklist

- [ ] API starts without errors
- [ ] Bedrock service initializes
- [ ] Comprehend client initializes
- [ ] First request succeeds
- [ ] NLP analysis completes
- [ ] Fraud scores are calculated
- [ ] Decision is made

### Post-Testing Checklist

- [ ] Response includes nlpAnalysis
- [ ] Summary is readable
- [ ] fraudRiskScore is 0.0-1.0
- [ ] Entities are extracted correctly
- [ ] Claim type is classified
- [ ] Combined fraud score is correct
- [ ] Final decision is appropriate
- [ ] Processing time is logged
- [ ] No errors in API logs
- [ ] No errors in AWS CloudWatch

---

## Conclusion

This implementation adds **AI-powered fraud detection** to the Claims Validation System by:

1. **Integrating AWS Bedrock** (Claude 3 Haiku) for narrative analysis
2. **Integrating AWS Comprehend** for entity extraction and sentiment
3. **Combining ML + NLP scores** with 60/40 weighting
4. **Maintaining backward compatibility** with fallback options
5. **Providing complete audit trail** with detailed logging

**Key Achievement**: System now analyzes both **structured data (ML)** and **narrative content (NLP)** for more accurate fraud detection.

