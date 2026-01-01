# AWS AI Components Integration Guide

## Overview

This guide provides complete code implementations for integrating AWS AI services into the Claims Validation System.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [AWS Textract (OCR)](#1-aws-textract-ocr)
3. [AWS SageMaker (ML Training & Inference)](#2-aws-sagemaker-ml-training--inference)
4. [AWS Bedrock (Fraud AI)](#3-aws-bedrock-fraud-ai)
5. [AWS SES (Email)](#4-aws-ses-email)
6. [AWS S3 (Storage)](#5-aws-s3-storage)
7. [Configuration](#6-configuration)
8. [Dependency Injection Setup](#7-dependency-injection-setup)
9. [Testing](#8-testing)

---

## Prerequisites

### 1. Install AWS NuGet Packages

```bash
# Navigate to your project directory
cd "c:\Hackathon Projects\src\Claims.Services"

# Install AWS SDK packages
dotnet add package AWSSDK.Textract --version 3.7.300
dotnet add package AWSSDK.SageMaker --version 3.7.300
dotnet add package AWSSDK.SageMakerRuntime --version 3.7.300
dotnet add package AWSSDK.BedrockRuntime --version 3.7.300
dotnet add package AWSSDK.SimpleEmail --version 3.7.300
dotnet add package AWSSDK.S3 --version 3.7.300
dotnet add package AWSSDK.Extensions.NETCore.Setup --version 3.7.300
```

### 2. Configure AWS Credentials

**Option A: AWS CLI (Recommended for Development)**
```bash
aws configure
# AWS Access Key ID: YOUR_ACCESS_KEY
# AWS Secret Access Key: YOUR_SECRET_KEY
# Default region name: us-east-1
# Default output format: json
```

**Option B: Environment Variables**
```bash
# Windows PowerShell
$env:AWS_ACCESS_KEY_ID="YOUR_ACCESS_KEY"
$env:AWS_SECRET_ACCESS_KEY="YOUR_SECRET_KEY"
$env:AWS_REGION="us-east-1"
```

**Option C: appsettings.json (Not Recommended for Production)**
```json
{
  "AWS": {
    "Profile": "default",
    "Region": "us-east-1"
  }
}
```

---

## 1. AWS Textract (OCR)

### Service Interface

Create `src/Claims.Services/Interfaces/IAwsTextractService.cs`:

```csharp
using Claims.Domain.Entities;

namespace Claims.Services.Interfaces;

public interface IAwsTextractService
{
    Task<(string ExtractedText, decimal Confidence)> ExtractTextFromS3Async(string bucketName, string key);
    Task<(string ExtractedText, decimal Confidence)> ExtractTextFromBytesAsync(byte[] documentBytes, string documentName);
    Task<Dictionary<string, string>> ExtractFormDataAsync(string bucketName, string key);
    Task<List<TableData>> ExtractTablesAsync(string bucketName, string key);
}

public class TableData
{
    public int PageNumber { get; set; }
    public List<List<string>> Rows { get; set; } = new();
}
```

### Service Implementation

Create `src/Claims.Services/Implementations/AwsTextractService.cs`:

```csharp
using Amazon.Textract;
using Amazon.Textract.Model;
using Claims.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Claims.Services.Implementations;

public class AwsTextractService : IAwsTextractService
{
    private readonly IAmazonTextract _textractClient;
    private readonly ILogger<AwsTextractService> _logger;

    public AwsTextractService(IAmazonTextract textractClient, ILogger<AwsTextractService> logger)
    {
        _textractClient = textractClient;
        _logger = logger;
    }

    public async Task<(string ExtractedText, decimal Confidence)> ExtractTextFromS3Async(string bucketName, string key)
    {
        try
        {
            _logger.LogInformation("Starting Textract OCR for s3://{Bucket}/{Key}", bucketName, key);

            var request = new DetectDocumentTextRequest
            {
                Document = new Document
                {
                    S3Object = new S3Object
                    {
                        Bucket = bucketName,
                        Name = key
                    }
                }
            };

            var response = await _textractClient.DetectDocumentTextAsync(request);

            var textBuilder = new StringBuilder();
            var confidenceScores = new List<float>();

            foreach (var block in response.Blocks)
            {
                if (block.BlockType == BlockType.LINE)
                {
                    textBuilder.AppendLine(block.Text);
                    if (block.Confidence > 0)
                    {
                        confidenceScores.Add(block.Confidence);
                    }
                }
            }

            var averageConfidence = confidenceScores.Any() 
                ? confidenceScores.Average() / 100 
                : 0m;

            var extractedText = textBuilder.ToString().Trim();

            _logger.LogInformation("Textract OCR completed. Confidence: {Confidence:P2}", averageConfidence);

            return (extractedText, (decimal)averageConfidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Textract OCR failed for s3://{Bucket}/{Key}", bucketName, key);
            throw;
        }
    }

    public async Task<(string ExtractedText, decimal Confidence)> ExtractTextFromBytesAsync(byte[] documentBytes, string documentName)
    {
        try
        {
            _logger.LogInformation("Starting Textract OCR for document: {DocumentName}", documentName);

            var request = new DetectDocumentTextRequest
            {
                Document = new Document
                {
                    Bytes = new MemoryStream(documentBytes)
                }
            };

            var response = await _textractClient.DetectDocumentTextAsync(request);

            var textBuilder = new StringBuilder();
            var confidenceScores = new List<float>();

            foreach (var block in response.Blocks)
            {
                if (block.BlockType == BlockType.LINE)
                {
                    textBuilder.AppendLine(block.Text);
                    if (block.Confidence > 0)
                    {
                        confidenceScores.Add(block.Confidence);
                    }
                }
            }

            var averageConfidence = confidenceScores.Any() 
                ? confidenceScores.Average() / 100 
                : 0m;

            var extractedText = textBuilder.ToString().Trim();

            _logger.LogInformation("Textract OCR completed. Confidence: {Confidence:P2}", averageConfidence);

            return (extractedText, (decimal)averageConfidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Textract OCR failed for document: {DocumentName}", documentName);
            throw;
        }
    }

    public async Task<Dictionary<string, string>> ExtractFormDataAsync(string bucketName, string key)
    {
        try
        {
            _logger.LogInformation("Starting Textract form extraction for s3://{Bucket}/{Key}", bucketName, key);

            var request = new AnalyzeDocumentRequest
            {
                Document = new Document
                {
                    S3Object = new S3Object
                    {
                        Bucket = bucketName,
                        Name = key
                    }
                },
                FeatureTypes = new List<string> { "FORMS" }
            };

            var response = await _textractClient.AnalyzeDocumentAsync(request);

            var keyValuePairs = new Dictionary<string, string>();
            var blockMap = response.Blocks.ToDictionary(b => b.Id, b => b);

            foreach (var block in response.Blocks.Where(b => b.BlockType == BlockType.KEY_VALUE_SET))
            {
                if (block.EntityTypes.Contains("KEY"))
                {
                    var key = GetText(block, blockMap);
                    var valueBlock = GetValueBlock(block, blockMap);
                    var value = valueBlock != null ? GetText(valueBlock, blockMap) : "";

                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        keyValuePairs[key] = value;
                    }
                }
            }

            _logger.LogInformation("Extracted {Count} key-value pairs from form", keyValuePairs.Count);

            return keyValuePairs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Textract form extraction failed for s3://{Bucket}/{Key}", bucketName, key);
            throw;
        }
    }

    public async Task<List<TableData>> ExtractTablesAsync(string bucketName, string key)
    {
        try
        {
            _logger.LogInformation("Starting Textract table extraction for s3://{Bucket}/{Key}", bucketName, key);

            var request = new AnalyzeDocumentRequest
            {
                Document = new Document
                {
                    S3Object = new S3Object
                    {
                        Bucket = bucketName,
                        Name = key
                    }
                },
                FeatureTypes = new List<string> { "TABLES" }
            };

            var response = await _textractClient.AnalyzeDocumentAsync(request);

            var tables = new List<TableData>();
            var blockMap = response.Blocks.ToDictionary(b => b.Id, b => b);

            foreach (var block in response.Blocks.Where(b => b.BlockType == BlockType.TABLE))
            {
                var table = new TableData { PageNumber = block.Page };
                var cells = new Dictionary<(int row, int col), string>();

                if (block.Relationships != null)
                {
                    foreach (var relationship in block.Relationships)
                    {
                        if (relationship.Type == RelationshipType.CHILD)
                        {
                            foreach (var childId in relationship.Ids)
                            {
                                if (blockMap.TryGetValue(childId, out var cellBlock) && cellBlock.BlockType == BlockType.CELL)
                                {
                                    var cellText = GetText(cellBlock, blockMap);
                                    var rowIndex = cellBlock.RowIndex - 1;
                                    var colIndex = cellBlock.ColumnIndex - 1;
                                    cells[(rowIndex, colIndex)] = cellText;
                                }
                            }
                        }
                    }
                }

                if (cells.Any())
                {
                    var maxRow = cells.Keys.Max(k => k.row);
                    var maxCol = cells.Keys.Max(k => k.col);

                    for (int row = 0; row <= maxRow; row++)
                    {
                        var rowData = new List<string>();
                        for (int col = 0; col <= maxCol; col++)
                        {
                            rowData.Add(cells.TryGetValue((row, col), out var cellValue) ? cellValue : "");
                        }
                        table.Rows.Add(rowData);
                    }
                }

                tables.Add(table);
            }

            _logger.LogInformation("Extracted {Count} tables", tables.Count);

            return tables;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Textract table extraction failed for s3://{Bucket}/{Key}", bucketName, key);
            throw;
        }
    }

    private string GetText(Block block, Dictionary<string, Block> blockMap)
    {
        var text = new StringBuilder();

        if (block.Relationships != null)
        {
            foreach (var relationship in block.Relationships)
            {
                if (relationship.Type == RelationshipType.CHILD)
                {
                    foreach (var childId in relationship.Ids)
                    {
                        if (blockMap.TryGetValue(childId, out var childBlock))
                        {
                            if (childBlock.BlockType == BlockType.WORD || childBlock.BlockType == BlockType.SELECTION_ELEMENT)
                            {
                                text.Append(childBlock.Text + " ");
                            }
                        }
                    }
                }
            }
        }

        return text.ToString().Trim();
    }

    private Block? GetValueBlock(Block keyBlock, Dictionary<string, Block> blockMap)
    {
        if (keyBlock.Relationships == null) return null;

        foreach (var relationship in keyBlock.Relationships)
        {
            if (relationship.Type == RelationshipType.VALUE)
            {
                foreach (var valueId in relationship.Ids)
                {
                    if (blockMap.TryGetValue(valueId, out var valueBlock))
                    {
                        return valueBlock;
                    }
                }
            }
        }

        return null;
    }
}
```

### Update OcrService to Use Textract

Modify `src/Claims.Services/Implementations/OcrService.cs`:

```csharp
using Claims.Domain.Entities;
using Claims.Domain.Enums;
using Claims.Infrastructure.Data;
using Claims.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Claims.Services.Implementations;

public class OcrService : IOcrService
{
    private readonly IAwsTextractService _textractService;
    private readonly IAwsS3Service _s3Service;
    private readonly ClaimsDbContext _context;
    private readonly ILogger<OcrService> _logger;
    private readonly bool _useTextract;

    public OcrService(
        IAwsTextractService textractService,
        IAwsS3Service s3Service,
        ClaimsDbContext context,
        IConfiguration configuration,
        ILogger<OcrService> logger)
    {
        _textractService = textractService;
        _s3Service = s3Service;
        _context = context;
        _logger = logger;
        _useTextract = configuration.GetValue<bool>("AWS:UseTextract", true);
    }

    public async Task<Document> ProcessDocumentAsync(Claim claim, string blobUri, DocumentType documentType)
    {
        var document = new Document
        {
            DocumentId = Guid.NewGuid(),
            ClaimId = claim.ClaimId,
            DocumentType = documentType,
            BlobUri = blobUri,
            UploadedDate = DateTime.UtcNow,
            OcrStatus = OcrStatus.Pending
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        try
        {
            string extractedText;
            decimal confidence;

            if (_useTextract && blobUri.StartsWith("s3://"))
            {
                // Parse S3 URI
                var s3Parts = blobUri.Replace("s3://", "").Split('/', 2);
                var bucketName = s3Parts[0];
                var key = s3Parts[1];

                (extractedText, confidence) = await _textractService.ExtractTextFromS3Async(bucketName, key);
            }
            else
            {
                // Fallback to file-based OCR (Tesseract or download from S3)
                throw new NotImplementedException("File-based OCR fallback not implemented");
            }

            document.ExtractedText = extractedText;
            document.OcrConfidence = confidence;
            document.OcrStatus = OcrStatus.Completed;
            document.ProcessedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("OCR completed for document {DocumentId} with confidence {Confidence:P2}", 
                document.DocumentId, confidence);

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR processing failed for document {DocumentId}", document.DocumentId);

            document.OcrStatus = OcrStatus.Failed;
            await _context.SaveChangesAsync();

            throw;
        }
    }
}
```

---

## 2. AWS SageMaker (ML Training & Inference)

### Service Interface

Create `src/Claims.Services/Interfaces/IAwsSageMakerService.cs`:

```csharp
namespace Claims.Services.Interfaces;

public interface IAwsSageMakerService
{
    Task<decimal> PredictFraudScoreAsync(float amount, int documentCount, int claimantHistoryCount, int daysSinceLastClaim);
    Task<SageMakerEndpointInfo> GetEndpointInfoAsync();
}

public class SageMakerEndpointInfo
{
    public string EndpointName { get; set; }
    public string Status { get; set; }
    public DateTime? CreationTime { get; set; }
    public DateTime? LastModifiedTime { get; set; }
}
```

### Service Implementation

Create `src/Claims.Services/Implementations/AwsSageMakerService.cs`:

```csharp
using Amazon.SageMaker;
using Amazon.SageMaker.Model;
using Amazon.SageMakerRuntime;
using Amazon.SageMakerRuntime.Model;
using Claims.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Claims.Services.Implementations;

public class AwsSageMakerService : IAwsSageMakerService
{
    private readonly IAmazonSageMakerRuntime _sageMakerRuntimeClient;
    private readonly IAmazonSageMaker _sageMakerClient;
    private readonly ILogger<AwsSageMakerService> _logger;
    private readonly string _endpointName;

    public AwsSageMakerService(
        IAmazonSageMakerRuntime sageMakerRuntimeClient,
        IAmazonSageMaker sageMakerClient,
        IConfiguration configuration,
        ILogger<AwsSageMakerService> logger)
    {
        _sageMakerRuntimeClient = sageMakerRuntimeClient;
        _sageMakerClient = sageMakerClient;
        _logger = logger;
        _endpointName = configuration["AWS:SageMaker:EndpointName"] 
            ?? throw new InvalidOperationException("SageMaker endpoint name not configured");
    }

    public async Task<decimal> PredictFraudScoreAsync(
        float amount, 
        int documentCount, 
        int claimantHistoryCount, 
        int daysSinceLastClaim)
    {
        try
        {
            _logger.LogInformation("Invoking SageMaker endpoint: {EndpointName}", _endpointName);

            // Prepare input data in CSV format (adjust based on your model)
            var csvInput = $"{amount},{documentCount},{claimantHistoryCount},{daysSinceLastClaim}";

            // Alternative: JSON format
            // var jsonInput = JsonSerializer.Serialize(new
            // {
            //     instances = new[] { new { amount, documentCount, claimantHistoryCount, daysSinceLastClaim } }
            // });

            var request = new InvokeEndpointRequest
            {
                EndpointName = _endpointName,
                ContentType = "text/csv", // or "application/json"
                Body = new MemoryStream(Encoding.UTF8.GetBytes(csvInput))
            };

            var response = await _sageMakerRuntimeClient.InvokeEndpointAsync(request);

            using var reader = new StreamReader(response.Body);
            var resultString = await reader.ReadToEndAsync();

            _logger.LogInformation("SageMaker response: {Response}", resultString);

            // Parse response (format depends on your model output)
            // For binary classification, typically returns probability score
            if (decimal.TryParse(resultString.Trim(), out var fraudScore))
            {
                return Math.Clamp(fraudScore, 0m, 1m);
            }

            // Alternative: JSON response parsing
            // var result = JsonSerializer.Deserialize<SageMakerPrediction>(resultString);
            // return (decimal)result.predictions[0][0];

            _logger.LogWarning("Failed to parse SageMaker response, returning 0.5");
            return 0.5m; // Default to medium risk if parsing fails
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SageMaker prediction failed");
            throw;
        }
    }

    public async Task<SageMakerEndpointInfo> GetEndpointInfoAsync()
    {
        try
        {
            var request = new DescribeEndpointRequest
            {
                EndpointName = _endpointName
            };

            var response = await _sageMakerClient.DescribeEndpointAsync(request);

            return new SageMakerEndpointInfo
            {
                EndpointName = response.EndpointName,
                Status = response.EndpointStatus.Value,
                CreationTime = response.CreationTime,
                LastModifiedTime = response.LastModifiedTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SageMaker endpoint info");
            throw;
        }
    }
}

// Model classes for JSON response parsing
public class SageMakerPrediction
{
    public float[][] predictions { get; set; }
}
```

### Update MlScoringService

Modify `src/Claims.Services/Implementations/MlScoringService.cs`:

```csharp
using Claims.Domain.Entities;
using Claims.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Claims.Services.Implementations;

public class MlScoringService : IMlScoringService
{
    private readonly IAwsSageMakerService _sageMakerService;
    private readonly ILogger<MlScoringService> _logger;
    private readonly bool _useSageMaker;

    public MlScoringService(
        IAwsSageMakerService sageMakerService,
        IConfiguration configuration,
        ILogger<MlScoringService> logger)
    {
        _sageMakerService = sageMakerService;
        _logger = logger;
        _useSageMaker = configuration.GetValue<bool>("AWS:UseSageMaker", false);
    }

    public async Task<(decimal FraudScore, decimal ApprovalScore)> ScoreClaimAsync(Claim claim)
    {
        try
        {
            decimal fraudScore;

            if (_useSageMaker)
            {
                // Use SageMaker for prediction
                fraudScore = await _sageMakerService.PredictFraudScoreAsync(
                    amount: (float)claim.TotalAmount,
                    documentCount: claim.Documents?.Count ?? 0,
                    claimantHistoryCount: 0, // TODO: Get from database
                    daysSinceLastClaim: 0    // TODO: Calculate
                );
            }
            else
            {
                // Fallback to ML.NET or rule-based scoring
                fraudScore = CalculateRuleBasedFraudScore(claim);
            }

            // Approval score is inverse of fraud score
            decimal approvalScore = 1.0m - fraudScore;

            _logger.LogInformation(
                "Claim {ClaimId} scored - Fraud: {FraudScore:P2}, Approval: {ApprovalScore:P2}",
                claim.ClaimId, fraudScore, approvalScore);

            return (fraudScore, approvalScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scoring failed for claim {ClaimId}", claim.ClaimId);
            throw;
        }
    }

    public async Task<string> DetermineDecisionAsync(decimal fraudScore, decimal approvalScore)
    {
        await Task.CompletedTask;

        if (fraudScore > 0.7m)
            return "Reject";

        if (approvalScore > 0.8m && fraudScore < 0.3m)
            return "AutoApprove";

        return "ManualReview";
    }

    private decimal CalculateRuleBasedFraudScore(Claim claim)
    {
        decimal score = 0.0m;

        if (claim.TotalAmount > 5000) score += 0.3m;
        if (claim.Documents?.Count < 2) score += 0.25m;
        // Add more rules...

        return Math.Min(score, 1.0m);
    }
}
```

---

## 3. AWS Bedrock (Fraud AI)

### Service Interface

Create `src/Claims.Services/Interfaces/IAwsBedrockService.cs`:

```csharp
using Claims.Domain.Entities;

namespace Claims.Services.Interfaces;

public interface IAwsBedrockService
{
    Task<FraudAnalysisResult> AnalyzeClaimForFraudAsync(Claim claim);
    Task<string> ClassifyDocumentAsync(string extractedText);
    Task<List<string>> GenerateQuestionsAsync(Claim claim);
}

public class FraudAnalysisResult
{
    public decimal FraudScore { get; set; }
    public string Reasoning { get; set; }
    public List<string> RedFlags { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}
```

### Service Implementation

Create `src/Claims.Services/Implementations/AwsBedrockService.cs`:

```csharp
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Claims.Domain.Entities;
using Claims.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Claims.Services.Implementations;

public class AwsBedrockService : IAwsBedrockService
{
    private readonly IAmazonBedrockRuntime _bedrockClient;
    private readonly ILogger<AwsBedrockService> _logger;
    private readonly string _modelId;

    public AwsBedrockService(
        IAmazonBedrockRuntime bedrockClient,
        IConfiguration configuration,
        ILogger<AwsBedrockService> logger)
    {
        _bedrockClient = bedrockClient;
        _logger = logger;
        _modelId = configuration["AWS:Bedrock:ModelId"] 
            ?? "anthropic.claude-3-haiku-20240307-v1:0";
    }

    public async Task<FraudAnalysisResult> AnalyzeClaimForFraudAsync(Claim claim)
    {
        try
        {
            _logger.LogInformation("Analyzing claim {ClaimId} with Bedrock AI", claim.ClaimId);

            var prompt = $@"You are an expert insurance fraud detection analyst. Analyze the following insurance claim and provide a detailed fraud risk assessment.

CLAIM DETAILS:
- Policy ID: {claim.PolicyId}
- Claim Amount: ${claim.TotalAmount:N2}
- Submission Date: {claim.SubmittedDate:yyyy-MM-dd}
- Number of Documents: {claim.Documents?.Count ?? 0}
- Claim Status: {claim.Status}

DOCUMENTS:
{GetDocumentSummary(claim)}

TASK:
1. Analyze this claim for potential fraud indicators
2. Assign a fraud score between 0.0 (legitimate) and 1.0 (highly suspicious)
3. Provide clear reasoning for the score
4. List specific red flags if any
5. Provide recommendations for processing

Respond in JSON format:
{{
  ""fraudScore"": 0.0,
  ""reasoning"": ""Brief explanation of the assessment"",
  ""redFlags"": [""flag1"", ""flag2""],
  ""recommendations"": [""recommendation1"", ""recommendation2""]
}}";

            var response = await InvokeClaudeAsync(prompt);

            // Parse the JSON response
            var analysis = JsonSerializer.Deserialize<FraudAnalysisResult>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation("Bedrock fraud analysis complete. Score: {Score:P2}", analysis?.FraudScore ?? 0);

            return analysis ?? new FraudAnalysisResult { FraudScore = 0.5m, Reasoning = "Analysis failed" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bedrock fraud analysis failed for claim {ClaimId}", claim.ClaimId);
            throw;
        }
    }

    public async Task<string> ClassifyDocumentAsync(string extractedText)
    {
        try
        {
            _logger.LogInformation("Classifying document with Bedrock AI");

            var prompt = $@"You are a document classification expert. Classify the following document text into one of these categories:
- Invoice
- Receipt
- MedicalReport
- PolicyDocument
- ClaimForm
- Other

DOCUMENT TEXT:
{extractedText.Substring(0, Math.Min(extractedText.Length, 1000))}...

Respond with ONLY the category name, nothing else.";

            var classification = await InvokeClaudeAsync(prompt);

            _logger.LogInformation("Document classified as: {Classification}", classification.Trim());

            return classification.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document classification failed");
            return "Other";
        }
    }

    public async Task<List<string>> GenerateQuestionsAsync(Claim claim)
    {
        try
        {
            _logger.LogInformation("Generating follow-up questions for claim {ClaimId}", claim.ClaimId);

            var prompt = $@"You are an insurance claims specialist. Based on this claim, generate 3-5 relevant follow-up questions that would help validate the claim.

CLAIM DETAILS:
- Policy ID: {claim.PolicyId}
- Claim Amount: ${claim.TotalAmount:N2}
- Documents Provided: {claim.Documents?.Count ?? 0}

Generate questions in JSON array format:
[""question1"", ""question2"", ""question3""]";

            var response = await InvokeClaudeAsync(prompt);

            var questions = JsonSerializer.Deserialize<List<string>>(response);

            return questions ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Question generation failed");
            return new List<string>();
        }
    }

    private async Task<string> InvokeClaudeAsync(string prompt, int maxTokens = 1000)
    {
        var requestBody = new ClaudeRequest
        {
            AnthropicVersion = "bedrock-2023-05-31",
            MaxTokens = maxTokens,
            Messages = new[]
            {
                new ClaudeMessage
                {
                    Role = "user",
                    Content = prompt
                }
            }
        };

        var jsonRequest = JsonSerializer.Serialize(requestBody);

        var request = new InvokeModelRequest
        {
            ModelId = _modelId,
            ContentType = "application/json",
            Accept = "application/json",
            Body = new MemoryStream(Encoding.UTF8.GetBytes(jsonRequest))
        };

        var response = await _bedrockClient.InvokeModelAsync(request);

        using var reader = new StreamReader(response.Body);
        var responseJson = await reader.ReadToEndAsync();

        var claudeResponse = JsonSerializer.Deserialize<ClaudeResponse>(responseJson);

        return claudeResponse?.Content?.FirstOrDefault()?.Text ?? "";
    }

    private string GetDocumentSummary(Claim claim)
    {
        if (claim.Documents == null || !claim.Documents.Any())
            return "No documents attached";

        var summary = new StringBuilder();
        foreach (var doc in claim.Documents)
        {
            summary.AppendLine($"- {doc.DocumentType}: {doc.ExtractedText?.Substring(0, Math.Min(doc.ExtractedText.Length, 200))}...");
        }
        return summary.ToString();
    }
}

// Claude API models
public class ClaudeRequest
{
    [JsonPropertyName("anthropic_version")]
    public string AnthropicVersion { get; set; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("messages")]
    public ClaudeMessage[] Messages { get; set; }
}

public class ClaudeMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}

public class ClaudeResponse
{
    [JsonPropertyName("content")]
    public ClaudeContent[] Content { get; set; }

    [JsonPropertyName("usage")]
    public ClaudeUsage Usage { get; set; }
}

public class ClaudeContent
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}

public class ClaudeUsage
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }
}
```

---

## 4. AWS SES (Email)

### Service Interface

Update `src/Claims.Services/Interfaces/INotificationService.cs` (already exists):

```csharp
using Claims.Domain.Enums;

namespace Claims.Services.Interfaces;

public interface INotificationService
{
    Task SendEmailAsync(string recipientEmail, string subject, string body);
    Task SendClaimStatusNotificationAsync(Guid claimId, string recipientEmail, NotificationType notificationType);
    Task SendTemplatedEmailAsync(string recipientEmail, string templateName, Dictionary<string, string> templateData);
}
```

### Service Implementation

Create `src/Claims.Services/Implementations/AwsSesNotificationService.cs`:

```csharp
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Claims.Domain.Entities;
using Claims.Domain.Enums;
using Claims.Infrastructure.Data;
using Claims.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Claims.Services.Implementations;

public class AwsSesNotificationService : INotificationService
{
    private readonly IAmazonSimpleEmailService _sesClient;
    private readonly ClaimsDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AwsSesNotificationService> _logger;
    private readonly string _sourceEmail;

    public AwsSesNotificationService(
        IAmazonSimpleEmailService sesClient,
        ClaimsDbContext context,
        IConfiguration configuration,
        ILogger<AwsSesNotificationService> logger)
    {
        _sesClient = sesClient;
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _sourceEmail = configuration["AWS:SES:SourceEmail"] 
            ?? "noreply@claims.com";
    }

    public async Task SendEmailAsync(string recipientEmail, string subject, string body)
    {
        try
        {
            _logger.LogInformation("Sending email to {Email} via AWS SES", recipientEmail);

            var request = new SendEmailRequest
            {
                Source = _sourceEmail,
                Destination = new Destination
                {
                    ToAddresses = new List<string> { recipientEmail }
                },
                Message = new Message
                {
                    Subject = new Content(subject),
                    Body = new Body
                    {
                        Html = new Content
                        {
                            Charset = "UTF-8",
                            Data = body
                        }
                    }
                }
            };

            var response = await _sesClient.SendEmailAsync(request);

            _logger.LogInformation("Email sent successfully. MessageId: {MessageId}", response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", recipientEmail);
            throw;
        }
    }

    public async Task SendClaimStatusNotificationAsync(
        Guid claimId,
        string recipientEmail,
        NotificationType notificationType)
    {
        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            ClaimId = claimId,
            RecipientEmail = recipientEmail,
            NotificationType = notificationType,
            SentDate = DateTime.UtcNow,
            Status = NotificationStatus.Pending,
            MessageBody = GetNotificationMessage(notificationType)
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        try
        {
            await SendEmailAsync(
                recipientEmail,
                GetSubject(notificationType),
                BuildHtmlEmailBody(notification.MessageBody, claimId));

            notification.Status = NotificationStatus.Sent;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Notification sent for claim {ClaimId}", claimId);
        }
        catch (Exception ex)
        {
            notification.Status = NotificationStatus.Failed;
            await _context.SaveChangesAsync();

            _logger.LogError(ex, "Failed to send notification for claim {ClaimId}", claimId);
            throw;
        }
    }

    public async Task SendTemplatedEmailAsync(
        string recipientEmail,
        string templateName,
        Dictionary<string, string> templateData)
    {
        try
        {
            _logger.LogInformation("Sending templated email {Template} to {Email}", templateName, recipientEmail);

            var request = new SendTemplatedEmailRequest
            {
                Source = _sourceEmail,
                Destination = new Destination
                {
                    ToAddresses = new List<string> { recipientEmail }
                },
                Template = templateName,
                TemplateData = System.Text.Json.JsonSerializer.Serialize(templateData)
            };

            var response = await _sesClient.SendTemplatedEmailAsync(request);

            _logger.LogInformation("Templated email sent. MessageId: {MessageId}", response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send templated email to {Email}", recipientEmail);
            throw;
        }
    }

    private string GetNotificationMessage(NotificationType type)
    {
        return type switch
        {
            NotificationType.ClaimReceived => "Your claim has been received and is being processed.",
            NotificationType.StatusUpdate => "Your claim status has been updated.",
            NotificationType.DecisionMade => "A decision has been made on your claim.",
            NotificationType.DocumentsRequested => "Additional documents are required for your claim.",
            NotificationType.ManualReviewAssigned => "Your claim has been assigned for manual review.",
            _ => "Claim notification"
        };
    }

    private string GetSubject(NotificationType type)
    {
        return type switch
        {
            NotificationType.ClaimReceived => "Claim Received - Confirmation",
            NotificationType.StatusUpdate => "Claim Status Update",
            NotificationType.DecisionMade => "Claim Decision Notification",
            NotificationType.DocumentsRequested => "Additional Documents Required",
            NotificationType.ManualReviewAssigned => "Claim Under Manual Review",
            _ => "Claim Notification"
        };
    }

    private string BuildHtmlEmailBody(string message, Guid claimId)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #0066cc; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Claims Validation System</h1>
        </div>
        <div class='content'>
            <p>Dear Claimant,</p>
            <p>{message}</p>
            <p><strong>Claim ID:</strong> {claimId}</p>
            <p>If you have any questions, please contact our support team.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 Claims Validation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }
}
```

---

## 5. AWS S3 (Storage)

### Service Interface

Create `src/Claims.Services/Interfaces/IAwsS3Service.cs`:

```csharp
namespace Claims.Services.Interfaces;

public interface IAwsS3Service
{
    Task<string> UploadDocumentAsync(Stream fileStream, string fileName, string contentType);
    Task<Stream> DownloadDocumentAsync(string s3Key);
    Task<string> GeneratePreSignedUrlAsync(string s3Key, int expirationMinutes = 60);
    Task<bool> DeleteDocumentAsync(string s3Key);
    Task<List<string>> ListDocumentsAsync(string prefix = "");
}
```

### Service Implementation

Create `src/Claims.Services/Implementations/AwsS3Service.cs`:

```csharp
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Claims.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Claims.Services.Implementations;

public class AwsS3Service : IAwsS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<AwsS3Service> _logger;
    private readonly string _bucketName;

    public AwsS3Service(
        IAmazonS3 s3Client,
        IConfiguration configuration,
        ILogger<AwsS3Service> logger)
    {
        _s3Client = s3Client;
        _logger = logger;
        _bucketName = configuration["AWS:S3:BucketName"] 
            ?? throw new InvalidOperationException("S3 bucket name not configured");
    }

    public async Task<string> UploadDocumentAsync(Stream fileStream, string fileName, string contentType)
    {
        try
        {
            var key = $"claims/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}/{fileName}";

            _logger.LogInformation("Uploading document to S3: {Key}", key);

            var transferUtility = new TransferUtility(_s3Client);

            var uploadRequest = new TransferUtilityUploadRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = fileStream,
                ContentType = contentType,
                CannedACL = S3CannedACL.Private
            };

            await transferUtility.UploadAsync(uploadRequest);

            _logger.LogInformation("Document uploaded successfully to s3://{Bucket}/{Key}", _bucketName, key);

            return $"s3://{_bucketName}/{key}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload document {FileName} to S3", fileName);
            throw;
        }
    }

    public async Task<Stream> DownloadDocumentAsync(string s3Key)
    {
        try
        {
            // Remove s3:// prefix if present
            var key = s3Key.StartsWith("s3://") 
                ? s3Key.Replace($"s3://{_bucketName}/", "") 
                : s3Key;

            _logger.LogInformation("Downloading document from S3: {Key}", key);

            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectAsync(request);

            // Copy to memory stream to ensure stream is seekable
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            _logger.LogInformation("Document downloaded successfully");

            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download document from S3: {Key}", s3Key);
            throw;
        }
    }

    public async Task<string> GeneratePreSignedUrlAsync(string s3Key, int expirationMinutes = 60)
    {
        try
        {
            var key = s3Key.StartsWith("s3://") 
                ? s3Key.Replace($"s3://{_bucketName}/", "") 
                : s3Key;

            _logger.LogInformation("Generating pre-signed URL for: {Key}", key);

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Verb = HttpVerb.GET
            };

            var url = _s3Client.GetPreSignedURL(request);

            _logger.LogInformation("Pre-signed URL generated (expires in {Minutes} minutes)", expirationMinutes);

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate pre-signed URL for: {Key}", s3Key);
            throw;
        }
    }

    public async Task<bool> DeleteDocumentAsync(string s3Key)
    {
        try
        {
            var key = s3Key.StartsWith("s3://") 
                ? s3Key.Replace($"s3://{_bucketName}/", "") 
                : s3Key;

            _logger.LogInformation("Deleting document from S3: {Key}", key);

            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(request);

            _logger.LogInformation("Document deleted successfully");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document from S3: {Key}", s3Key);
            return false;
        }
    }

    public async Task<List<string>> ListDocumentsAsync(string prefix = "")
    {
        try
        {
            _logger.LogInformation("Listing documents in S3 with prefix: {Prefix}", prefix);

            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = prefix
            };

            var response = await _s3Client.ListObjectsV2Async(request);

            var keys = response.S3Objects.Select(obj => $"s3://{_bucketName}/{obj.Key}").ToList();

            _logger.LogInformation("Found {Count} documents", keys.Count);

            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list documents in S3");
            throw;
        }
    }
}
```

---

## 6. Configuration

### Update `appsettings.json`

Add to `src/Claims.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ClaimsDb;Trusted_Connection=True;"
  },
  "AWS": {
    "Profile": "default",
    "Region": "us-east-1",
    "UseTextract": true,
    "UseSageMaker": false,
    "UseBedrock": false,
    "S3": {
      "BucketName": "claims-documents-prod"
    },
    "Textract": {
      "MaxPages": 10
    },
    "SageMaker": {
      "EndpointName": "fraud-detection-endpoint-v1"
    },
    "Bedrock": {
      "ModelId": "anthropic.claude-3-haiku-20240307-v1:0"
    },
    "SES": {
      "SourceEmail": "noreply@yourdomain.com",
      "ReplyToEmail": "support@yourdomain.com"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Amazon": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

### Update `appsettings.Development.json`

```json
{
  "AWS": {
    "UseTextract": false,
    "UseSageMaker": false,
    "UseBedrock": false,
    "S3": {
      "BucketName": "claims-documents-dev"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning",
      "Amazon": "Debug"
    }
  }
}
```

---

## 7. Dependency Injection Setup

### Update `Program.cs`

Modify `src/Claims.Api/Program.cs`:

```csharp
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.S3;
using Amazon.SageMaker;
using Amazon.SageMakerRuntime;
using Amazon.SimpleEmail;
using Amazon.Textract;
using Claims.Infrastructure.Data;
using Claims.Services.Implementations;
using Claims.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<ClaimsDbContext>(options =>
    options.UseInMemoryDatabase("ClaimsDb"));

// AWS Configuration
var awsOptions = builder.Configuration.GetAWSOptions();
awsOptions.Region = RegionEndpoint.GetBySystemName(
    builder.Configuration["AWS:Region"] ?? "us-east-1");

// AWS Clients
builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddAWSService<IAmazonTextract>();
builder.Services.AddAWSService<IAmazonSageMaker>();
builder.Services.AddAWSService<IAmazonSageMakerRuntime>();
builder.Services.AddAWSService<IAmazonBedrockRuntime>();
builder.Services.AddAWSService<IAmazonSimpleEmailService>();
builder.Services.AddAWSService<IAmazonS3>();

// Application Services
builder.Services.AddScoped<IClaimsService, ClaimsService>();
builder.Services.AddScoped<IOcrService, OcrService>();
builder.Services.AddScoped<IMlScoringService, MlScoringService>();
builder.Services.AddScoped<IDocumentAnalysisService, DocumentAnalysisService>();
builder.Services.AddScoped<IRulesEngineService, RulesEngineService>();
builder.Services.AddScoped<INotificationService, AwsSesNotificationService>();

// AWS Services
builder.Services.AddScoped<IAwsTextractService, AwsTextractService>();
builder.Services.AddScoped<IAwsSageMakerService, AwsSageMakerService>();
builder.Services.AddScoped<IAwsBedrockService, AwsBedrockService>();
builder.Services.AddScoped<IAwsS3Service, AwsS3Service>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## 8. Testing

### Create Test Controller

Add to `src/Claims.Api/Controllers/AwsTestController.cs`:

```csharp
using Claims.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AwsTestController : ControllerBase
{
    private readonly IAwsTextractService _textractService;
    private readonly IAwsSageMakerService _sageMakerService;
    private readonly IAwsBedrockService _bedrockService;
    private readonly IAwsS3Service _s3Service;
    private readonly ILogger<AwsTestController> _logger;

    public AwsTestController(
        IAwsTextractService textractService,
        IAwsSageMakerService sageMakerService,
        IAwsBedrockService bedrockService,
        IAwsS3Service s3Service,
        ILogger<AwsTestController> logger)
    {
        _textractService = textractService;
        _sageMakerService = sageMakerService;
        _bedrockService = bedrockService;
        _s3Service = s3Service;
        _logger = logger;
    }

    [HttpGet("textract/test")]
    public async Task<IActionResult> TestTextract([FromQuery] string bucket, [FromQuery] string key)
    {
        try
        {
            var result = await _textractService.ExtractTextFromS3Async(bucket, key);
            return Ok(new
            {
                extractedText = result.ExtractedText,
                confidence = result.Confidence,
                success = true
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("sagemaker/test")]
    public async Task<IActionResult> TestSageMaker()
    {
        try
        {
            var fraudScore = await _sageMakerService.PredictFraudScoreAsync(
                amount: 5000,
                documentCount: 2,
                claimantHistoryCount: 1,
                daysSinceLastClaim: 30
            );

            var endpointInfo = await _sageMakerService.GetEndpointInfoAsync();

            return Ok(new
            {
                fraudScore,
                endpointInfo,
                success = true
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("bedrock/test")]
    public async Task<IActionResult> TestBedrock([FromBody] string prompt)
    {
        try
        {
            var classification = await _bedrockService.ClassifyDocumentAsync(prompt);

            return Ok(new
            {
                classification,
                success = true
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("s3/list")]
    public async Task<IActionResult> ListS3Documents([FromQuery] string? prefix)
    {
        try
        {
            var documents = await _s3Service.ListDocumentsAsync(prefix ?? "");

            return Ok(new
            {
                documents,
                count = documents.Count,
                success = true
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("s3/presigned-url")]
    public async Task<IActionResult> GeneratePresignedUrl([FromQuery] string key)
    {
        try
        {
            var url = await _s3Service.GeneratePreSignedUrlAsync(key, 60);

            return Ok(new
            {
                url,
                expiresIn = "60 minutes",
                success = true
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
```

### Test Commands

```bash
# Test S3 List
curl http://localhost:5000/api/awstest/s3/list

# Test Textract
curl "http://localhost:5000/api/awstest/textract/test?bucket=claims-documents-dev&key=test.pdf"

# Test SageMaker
curl http://localhost:5000/api/awstest/sagemaker/test

# Test Bedrock
curl -X POST http://localhost:5000/api/awstest/bedrock/test \
  -H "Content-Type: application/json" \
  -d "\"This is a medical invoice for $5000 dated January 15, 2025\""

# Test S3 Pre-signed URL
curl "http://localhost:5000/api/awstest/s3/presigned-url?key=claims/2025/01/01/test.pdf"
```

---

## 9. AWS Resource Setup

### Create S3 Bucket

```bash
# Create S3 bucket
aws s3 mb s3://claims-documents-prod --region us-east-1

# Enable versioning
aws s3api put-bucket-versioning \
  --bucket claims-documents-prod \
  --versioning-configuration Status=Enabled

# Set lifecycle policy (optional)
aws s3api put-bucket-lifecycle-configuration \
  --bucket claims-documents-prod \
  --lifecycle-configuration file://s3-lifecycle.json
```

### Verify SES Email

```bash
# Verify email address
aws ses verify-email-identity --email-address noreply@yourdomain.com

# Check verification status
aws ses get-identity-verification-attributes \
  --identities noreply@yourdomain.com
```

### Check SageMaker Endpoint

```bash
# List endpoints
aws sagemaker list-endpoints

# Describe endpoint
aws sagemaker describe-endpoint --endpoint-name fraud-detection-endpoint-v1
```

---

## 10. Cost Optimization Tips

1. **Textract**: Use `DetectDocumentText` (cheaper) instead of `AnalyzeDocument` unless you need tables/forms
2. **SageMaker**: Use serverless inference or stop endpoints when not in use
3. **Bedrock**: Use Claude 3 Haiku (cheapest) for most tasks, reserve Opus for complex analysis
4. **S3**: Enable lifecycle policies to move old documents to Glacier
5. **SES**: Stay in sandbox mode during development (free tier)

---

## 11. Next Steps

1. ✅ Install AWS SDKs
2. ✅ Configure AWS credentials
3. ✅ Update appsettings.json
4. ✅ Implement service interfaces
5. ✅ Register services in Program.cs
6. ✅ Test individual components
7. 🔄 Create S3 bucket
8. 🔄 Verify SES email
9. 🔄 Deploy SageMaker endpoint
10. 🔄 End-to-end integration testing

---

**Last Updated**: December 27, 2025  
**Status**: Ready for Capstone Implementation  
**Tech Stack**: .NET 8.0 + AWS AI Services
