# Azure Integration Guide for Claims Validation System

> **Last Updated**: December 29, 2025  
> **Platform**: .NET 9.0 / ASP.NET Core Web API  
> **Cloud Provider**: Microsoft Azure

---

## Table of Contents

1. [Overview](#overview)
2. [Azure Services Used](#azure-services-used)
3. [Prerequisites](#prerequisites)
4. [Configuration](#configuration)
5. [Service Implementations](#service-implementations)
6. [Feature Flags](#feature-flags)
7. [Getting Started](#getting-started)
8. [NuGet Packages](#nuget-packages)
9. [Cost Estimates](#cost-estimates)
10. [Troubleshooting](#troubleshooting)

---

## Overview

This Claims Validation System integrates with multiple Azure AI and cloud services to provide:

- **Document Intelligence**: Advanced OCR with structured data extraction
- **Azure OpenAI (GPT-4)**: NLP for summarization, entity extraction, fraud analysis
- **Blob Storage**: Secure document storage with versioning
- **Communication Services**: Email notifications for claim status updates
- **Application Insights**: Performance monitoring and error tracking

The system is designed with **feature flags** allowing you to enable/disable Azure services and fall back to local alternatives (Tesseract OCR, local file storage, console logging).

---

## Azure Services Used

| Service | Purpose | Local Fallback |
|---------|---------|----------------|
| **Azure AI Document Intelligence** | Extract text, tables, key-value pairs from documents | Tesseract OCR |
| **Azure OpenAI** | Claim summarization, entity extraction, fraud narrative analysis | Basic regex patterns |
| **Azure AI Language** | Entity recognition, sentiment analysis | Keyword matching |
| **Azure Blob Storage** | Store uploaded claim documents | Local file system |
| **Azure Communication Services** | Send email notifications | Console logging / MailKit SMTP |
| **Application Insights** | Telemetry, logging, performance monitoring | Console logging |

---

## Prerequisites

### Azure Account Setup

1. **Create Azure Account**: https://azure.microsoft.com/free/
   - New accounts get $200 free credit (valid 30 days)

2. **Required Azure Resources**:
   ```
   ├── Resource Group: ClaimsValidation-RG
   │   ├── Azure AI Document Intelligence (Form Recognizer)
   │   ├── Azure OpenAI Service (requires approval)
   │   ├── Azure AI Language
   │   ├── Storage Account (Blob)
   │   ├── Azure Communication Services
   │   └── Application Insights
   ```

### Azure CLI Commands to Create Resources

```bash
# Login to Azure
az login

# Create resource group
az group create --name ClaimsValidation-RG --location eastus

# Create Document Intelligence (Form Recognizer)
az cognitiveservices account create \
  --name claims-doc-intelligence \
  --resource-group ClaimsValidation-RG \
  --kind FormRecognizer \
  --sku S0 \
  --location eastus

# Create Azure OpenAI (Note: Requires application approval)
az cognitiveservices account create \
  --name claims-openai \
  --resource-group ClaimsValidation-RG \
  --kind OpenAI \
  --sku S0 \
  --location eastus

# Create AI Language
az cognitiveservices account create \
  --name claims-language \
  --resource-group ClaimsValidation-RG \
  --kind TextAnalytics \
  --sku S \
  --location eastus

# Create Storage Account
az storage account create \
  --name claimsdocstorage \
  --resource-group ClaimsValidation-RG \
  --sku Standard_LRS \
  --kind StorageV2

# Create Communication Services
az communication create \
  --name claims-communication \
  --resource-group ClaimsValidation-RG \
  --data-location unitedstates

# Create Application Insights
az monitor app-insights component create \
  --app claims-insights \
  --resource-group ClaimsValidation-RG \
  --location eastus
```

---

## Configuration

### appsettings.json Structure

The configuration is organized into sections for each Azure service:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information",
      "Claims.Services": "Debug"
    }
  },
  "AllowedHosts": "*",
  
  "ConnectionStrings": {
    "ClaimsDb": "",
    "AzureStorage": ""
  },

  "TesseractSettings": {
    "TessdataPath": "../../tessdata",
    "Enabled": true
  },

  "MLSettings": {
    "FraudModelPath": "../../MLModels/fraud-model.zip",
    "TrainingDataPath": "../../MLModels/claims-training-data.csv",
    "MinimumTrainingRows": 100,
    "RetrainThreshold": 0.05
  },

  "RulesEngine": {
    "MaxClaimAmount": 50000,
    "MaxClaimsPerMonth": 5,
    "MinDaysBetweenClaims": 3
  },

  "DecisionThresholds": {
    "FraudScoreRejectThreshold": 0.7,
    "ApprovalScoreAutoApproveThreshold": 0.8,
    "LowFraudThreshold": 0.3
  },

  "Azure": {
    "DocumentIntelligence": {
      "Endpoint": "",
      "ApiKey": "",
      "ModelId": "prebuilt-invoice",
      "Enabled": false
    },
    "OpenAI": {
      "Endpoint": "",
      "ApiKey": "",
      "DeploymentName": "gpt-4",
      "ApiVersion": "2024-02-15-preview",
      "Enabled": false
    },
    "Language": {
      "Endpoint": "",
      "ApiKey": "",
      "Enabled": false
    },
    "BlobStorage": {
      "ConnectionString": "",
      "RawDocsContainer": "raw-docs",
      "ProcessedDocsContainer": "processed-docs",
      "Enabled": false
    },
    "CommunicationServices": {
      "ConnectionString": "",
      "SenderEmail": "DoNotReply@claims.azurecomm.net",
      "Enabled": false
    },
    "ApplicationInsights": {
      "ConnectionString": "",
      "Enabled": false
    }
  },

  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "",
    "Password": "",
    "SenderName": "Claims Validation System",
    "SenderEmail": "noreply@claims.com",
    "EnableSsl": true,
    "Enabled": false
  },

  "FeatureFlags": {
    "UseAzureDocumentIntelligence": false,
    "UseAzureOpenAI": false,
    "UseAzureBlobStorage": false,
    "UseTesseractOCR": true,
    "UseLocalMLModel": true,
    "SendEmailNotifications": false
  }
}
```

### How to Get Azure Credentials

#### 1. Document Intelligence (Form Recognizer)

```bash
# Get endpoint and key
az cognitiveservices account show \
  --name claims-doc-intelligence \
  --resource-group ClaimsValidation-RG \
  --query "properties.endpoint" -o tsv

az cognitiveservices account keys list \
  --name claims-doc-intelligence \
  --resource-group ClaimsValidation-RG \
  --query "key1" -o tsv
```

**Configuration:**
```json
"DocumentIntelligence": {
  "Endpoint": "https://claims-doc-intelligence.cognitiveservices.azure.com/",
  "ApiKey": "your-api-key-here",
  "ModelId": "prebuilt-invoice",
  "Enabled": true
}
```

#### 2. Azure OpenAI

```bash
# Get endpoint
az cognitiveservices account show \
  --name claims-openai \
  --resource-group ClaimsValidation-RG \
  --query "properties.endpoint" -o tsv

# Get key
az cognitiveservices account keys list \
  --name claims-openai \
  --resource-group ClaimsValidation-RG \
  --query "key1" -o tsv
```

**Configuration:**
```json
"OpenAI": {
  "Endpoint": "https://claims-openai.openai.azure.com/",
  "ApiKey": "your-api-key-here",
  "DeploymentName": "gpt-4",
  "ApiVersion": "2024-02-15-preview",
  "Enabled": true
}
```

> **Note**: After creating the Azure OpenAI resource, you need to deploy a model (e.g., gpt-4) via Azure OpenAI Studio.

#### 3. Blob Storage

```bash
# Get connection string
az storage account show-connection-string \
  --name claimsdocstorage \
  --resource-group ClaimsValidation-RG \
  --query "connectionString" -o tsv
```

**Configuration:**
```json
"BlobStorage": {
  "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=claimsdocstorage;AccountKey=...;EndpointSuffix=core.windows.net",
  "RawDocsContainer": "raw-docs",
  "ProcessedDocsContainer": "processed-docs",
  "Enabled": true
}
```

#### 4. Communication Services

```bash
# Get connection string
az communication list-key \
  --name claims-communication \
  --resource-group ClaimsValidation-RG \
  --query "primaryConnectionString" -o tsv
```

**Configuration:**
```json
"CommunicationServices": {
  "ConnectionString": "endpoint=https://claims-communication.communication.azure.com/;accesskey=...",
  "SenderEmail": "DoNotReply@your-domain.azurecomm.net",
  "Enabled": true
}
```

> **Note**: You need to configure a verified domain in Azure Communication Services to send emails.

#### 5. Application Insights

```bash
# Get connection string
az monitor app-insights component show \
  --app claims-insights \
  --resource-group ClaimsValidation-RG \
  --query "connectionString" -o tsv
```

**Configuration:**
```json
"ApplicationInsights": {
  "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/",
  "Enabled": true
}
```

---

## Service Implementations

### Azure Service Files Location

```
src/Claims.Services/
├── Azure/
│   ├── AzureDocumentIntelligenceService.cs
│   ├── AzureOpenAIService.cs
│   ├── AzureBlobStorageService.cs
│   └── AzureEmailService.cs
├── Implementations/
│   ├── ClaimsService.cs
│   ├── OcrService.cs (Tesseract fallback)
│   ├── DocumentAnalysisService.cs
│   ├── RulesEngineService.cs
│   ├── MlScoringService.cs
│   └── NotificationService.cs
└── Interfaces/
    ├── IClaimsService.cs
    ├── IOcrService.cs
    └── ...
```

### 1. AzureDocumentIntelligenceService.cs

**Purpose**: Advanced OCR with structured data extraction

**Key Methods**:
- `ProcessDocumentAsync(string documentPath)` - Extract text with confidence score
- `ExtractInvoiceDataAsync(string documentPath)` - Extract structured invoice data

**Features**:
- Table extraction
- Key-value pair extraction (Invoice #, Date, Amount)
- Signature detection
- Multi-language support
- Prebuilt models for invoices, receipts, insurance forms

**Usage Example**:
```csharp
var (text, confidence) = await _ocrService.ProcessDocumentAsync("path/to/document.pdf");
Console.WriteLine($"Extracted text with {confidence:P2} confidence");
```

### 2. AzureOpenAIService.cs

**Purpose**: GPT-4 powered NLP for claims processing

**Key Methods**:
- `SummarizeClaimAsync(string description, string documentText)` - Generate claim summary
- `ExtractEntitiesAsync(string text)` - Extract names, dates, amounts, locations
- `AnalyzeFraudNarrativeAsync(string description)` - Detect fraud indicators in text
- `GenerateClaimResponseAsync(...)` - Generate professional response emails

**Usage Example**:
```csharp
var summary = await _openAIService.SummarizeClaimAsync(claim.Description, documentText);
var entities = await _openAIService.ExtractEntitiesAsync(documentText);
var fraudAnalysis = await _openAIService.AnalyzeFraudNarrativeAsync(claim.Description);
```

### 3. AzureBlobStorageService.cs

**Purpose**: Secure document storage

**Key Methods**:
- `UploadDocumentAsync(Guid claimId, string fileName, Stream content)` - Upload document
- `DownloadDocumentAsync(string blobUri)` - Download document
- `GetClaimDocumentsAsync(Guid claimId)` - List all documents for a claim
- `DeleteDocumentAsync(string blobUri)` - Delete document
- `MoveToProcessedAsync(string rawBlobUri, Guid claimId)` - Move to processed folder

**Storage Structure**:
```
raw-docs/
├── {claimId}/
│   ├── {guid}/{filename.pdf}
│   └── {guid}/{filename.jpg}
processed-docs/
├── {claimId}/
│   └── {date}/{filename.pdf}
```

### 4. AzureEmailService.cs

**Purpose**: Professional email notifications

**Key Methods**:
- `SendEmailAsync(string recipient, string subject, string htmlBody)` - Send generic email
- `SendClaimNotificationAsync(...)` - Send claim status update
- `SendDecisionNotificationAsync(...)` - Send claim decision with styled HTML

**Email Templates**: Professional HTML templates with:
- Company branding header
- Status color coding (green=approved, red=rejected, yellow=pending)
- Responsive design
- Plain text fallback

---

## Feature Flags

Feature flags allow you to enable/disable services without code changes:

```json
"FeatureFlags": {
  "UseAzureDocumentIntelligence": false,  // true = Azure, false = Tesseract
  "UseAzureOpenAI": false,                // true = GPT-4, false = basic NLP
  "UseAzureBlobStorage": false,           // true = Azure Blob, false = local
  "UseTesseractOCR": true,                // Fallback OCR
  "UseLocalMLModel": true,                // ML.NET fraud detection
  "SendEmailNotifications": false         // Enable email sending
}
```

### Service Registration (Program.cs)

```csharp
// Register OCR Service based on feature flag
var useAzureDocumentIntelligence = builder.Configuration
    .GetValue<bool>("FeatureFlags:UseAzureDocumentIntelligence");

if (useAzureDocumentIntelligence)
{
    builder.Services.AddScoped<IOcrService, AzureDocumentIntelligenceService>();
}
else
{
    builder.Services.AddScoped<IOcrService, OcrService>(); // Tesseract
}

// Register Azure Services (available when enabled)
builder.Services.AddSingleton<AzureOpenAIService>();
builder.Services.AddSingleton<AzureBlobStorageService>();
builder.Services.AddSingleton<AzureEmailService>();

// Application Insights
var appInsightsConnectionString = builder.Configuration["Azure:ApplicationInsights:ConnectionString"];
if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
}
```

---

## Getting Started

### Step 1: Clone and Restore

```bash
git clone <repository-url>
cd "Hackathon Projects"
dotnet restore
```

### Step 2: Configure Azure Services

1. Copy `appsettings.json` to `appsettings.Development.json`
2. Add your Azure credentials
3. Enable desired feature flags

### Step 3: Run Without Azure (Local Mode)

```bash
# Uses Tesseract OCR, in-memory database, console logging
dotnet run --project src/Claims.Api
```

### Step 4: Run With Azure Services

1. Set feature flags to `true`
2. Add Azure credentials
3. Run the application:

```bash
dotnet run --project src/Claims.Api
```

### Step 5: Verify Health Endpoint

```bash
curl http://localhost:5159/health
```

Response shows active features:
```json
{
  "status": "healthy",
  "service": "Claims.Api",
  "version": "1.0.0",
  "activeFeatures": {
    "AzureDocumentIntelligence": true,
    "AzureOpenAI": true,
    "AzureBlobStorage": true,
    "TesseractOCR": false,
    "LocalMLModel": true,
    "EmailNotifications": true
  }
}
```

---

## NuGet Packages

### Claims.Services.csproj

```xml
<ItemGroup>
  <!-- Existing packages -->
  <PackageReference Include="MailKit" Version="4.3.0" />
  <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="9.0.0" />
  <PackageReference Include="Microsoft.ML" Version="3.0.1" />
  <PackageReference Include="Microsoft.ML.FastTree" Version="3.0.1" />
  
  <!-- Azure AI Services -->
  <PackageReference Include="Azure.AI.FormRecognizer" Version="4.1.0" />
  <PackageReference Include="Azure.AI.OpenAI" Version="1.0.0-beta.17" />
  <PackageReference Include="Azure.AI.TextAnalytics" Version="5.3.0" />
  
  <!-- Azure Storage -->
  <PackageReference Include="Azure.Storage.Blobs" Version="12.19.1" />
  
  <!-- Azure Communication Services -->
  <PackageReference Include="Azure.Communication.Email" Version="1.0.1" />
</ItemGroup>
```

### Claims.Api.csproj

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.4" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
  <PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="2.22.0" />
</ItemGroup>
```

---

## Cost Estimates

### Development/Testing (Azure Free Tier)

| Service | Free Tier Limit | Notes |
|---------|----------------|-------|
| Document Intelligence | 500 pages/month | Sufficient for testing |
| Azure OpenAI | N/A | $200 credit for new accounts |
| Blob Storage | 5 GB | 12 months free |
| Communication Services | 1000 emails/month | Sufficient for testing |
| Application Insights | 5 GB/month | Free ingestion |

### Production Estimates

| Scale | Monthly Cost | Claims/Month |
|-------|-------------|--------------|
| Small | $50-100 | 100-500 |
| Medium | $150-300 | 500-2000 |
| Large | $500+ | 2000+ |

---

## Troubleshooting

### Common Issues

#### 1. "Azure Document Intelligence not enabled"
**Solution**: Check `Azure:DocumentIntelligence:Enabled` is `true` and credentials are set.

#### 2. "Azure OpenAI requires approval"
**Solution**: Apply for access at https://aka.ms/oai/access. Approval may take 1-2 business days.

#### 3. "Email sending failed"
**Solution**: Verify Communication Services domain is configured and verified.

#### 4. "Blob upload failed"
**Solution**: Check storage account connection string and ensure containers exist.

#### 5. "Application Insights not logging"
**Solution**: Verify connection string format (should start with `InstrumentationKey=`).

### Debug Logging

Enable detailed logging in `appsettings.Development.json`:

```json
"Logging": {
  "LogLevel": {
    "Default": "Debug",
    "Claims.Services.Azure": "Trace",
    "Azure": "Debug"
  }
}
```

### Test Azure Connectivity

```powershell
# Test Document Intelligence
$endpoint = "https://YOUR-RESOURCE.cognitiveservices.azure.com/"
$key = "YOUR-API-KEY"

Invoke-RestMethod -Uri "$endpoint/formrecognizer/info?api-version=2023-07-31" `
  -Headers @{ "Ocp-Apim-Subscription-Key" = $key }
```

---

## Additional Resources

- [Azure AI Document Intelligence Documentation](https://learn.microsoft.com/azure/ai-services/document-intelligence/)
- [Azure OpenAI Service Documentation](https://learn.microsoft.com/azure/ai-services/openai/)
- [Azure Blob Storage Documentation](https://learn.microsoft.com/azure/storage/blobs/)
- [Azure Communication Services Documentation](https://learn.microsoft.com/azure/communication-services/)
- [Application Insights Documentation](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/)

---

## Support

For issues with this integration:

1. Check the [Troubleshooting](#troubleshooting) section
2. Enable debug logging and check Application Insights
3. Verify Azure resource health in Azure Portal
4. Review Azure service quotas and limits

---

*This guide was generated for the Claims Validation Capstone Project - December 2025*
