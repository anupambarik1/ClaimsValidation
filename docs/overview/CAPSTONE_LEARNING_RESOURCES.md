# Capstone Learning Resources - Claims Validation System

## Overview

This guide provides comprehensive learning resources for upgrading the Claims Validation POC to a production-ready Capstone project using AWS and Azure services.

---

## 🎯 Target Architecture

| **Component** | **Service** | **Purpose** |
|--------------|-------------|-------------|
| **OCR** | AWS Textract | Document text extraction |
| **ML Training** | AWS SageMaker | Fraud model training |
| **ML Inference** | ML.NET on Azure | Real-time fraud scoring |
| **Fraud AI** | AWS Bedrock | AI-powered fraud analysis |
| **Email** | AWS SES | Notification delivery |
| **Database** | Azure SQL | Claims data persistence |
| **Storage** | AWS S3 | Document storage |
| **Hosting** | Azure App Service | API hosting |

**Estimated Monthly Cost**: $167-174 (for 5,000 claims/month)

---

## 📚 Learning Resources by Component

### 1. AWS Textract (OCR)

#### Official Documentation
- [AWS Textract Documentation](https://docs.aws.amazon.com/textract/) - Complete API reference (2-3 hours)
- [Textract Developer Guide](https://docs.aws.amazon.com/textract/latest/dg/what-is.html) - Step-by-step guide (1 hour)
- [AWS SDK for .NET - Textract](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/textract-examples.html) - C# code examples (1 hour)

#### Hands-on Tutorials
- [Extract Text from Documents](https://aws.amazon.com/getting-started/hands-on/extract-text-with-amazon-textract/) - Interactive lab (30 min)
- [AWS Textract Workshop (YouTube)](https://www.youtube.com/watch?v=SZdAMUzn7SE) - Full workshop (45 min)

#### Code Samples
- [AWS Textract Code Samples](https://github.com/aws-samples/amazon-textract-code-samples) - Real-world examples (2 hours)

#### NuGet Package
```bash
dotnet add package AWSSDK.Textract
```

#### Sample Implementation
```csharp
using Amazon.Textract;
using Amazon.Textract.Model;

public class TextractOcrService : IOcrService
{
    private readonly IAmazonTextract _textractClient;
    
    public TextractOcrService(IAmazonTextract textractClient)
    {
        _textractClient = textractClient;
    }
    
    public async Task<(string ExtractedText, decimal Confidence)> ExtractTextAsync(string s3BucketName, string s3Key)
    {
        var request = new DetectDocumentTextRequest
        {
            Document = new Document
            {
                S3Object = new S3Object
                {
                    Bucket = s3BucketName,
                    Name = s3Key
                }
            }
        };
        
        var response = await _textractClient.DetectDocumentTextAsync(request);
        
        var extractedText = string.Join("\n", response.Blocks
            .Where(b => b.BlockType == BlockType.LINE)
            .Select(b => b.Text));
            
        var avgConfidence = response.Blocks
            .Where(b => b.Confidence > 0)
            .Average(b => b.Confidence);
        
        return (extractedText, (decimal)avgConfidence / 100);
    }
}
```

**Estimated Learning Time**: 6-8 hours

---

### 2. AWS SageMaker (ML Training)

#### Official Documentation
- [SageMaker Documentation](https://docs.aws.amazon.com/sagemaker/) - Complete platform guide (4-5 hours)
- [SageMaker Studio Lab (Free)](https://studiolab.sagemaker.aws/) - No AWS account needed (1 hour)
- [Call SageMaker from .NET](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/sagemaker-examples.html) - C# examples (1 hour)

#### Hands-on Tutorials
- [Build, Train, Deploy ML Model](https://aws.amazon.com/getting-started/hands-on/build-train-deploy-machine-learning-model-sagemaker/) - End-to-end tutorial (2 hours)
- [SageMaker Fraud Detection Example](https://github.com/aws/amazon-sagemaker-examples/tree/main/introduction_to_applying_machine_learning/fraud_detection) - **Exact use case!** (3 hours)

#### Video Courses
- [AWS Training: SageMaker](https://explore.skillbuilder.aws/learn/course/external/view/elearning/1903/getting-started-with-amazon-sagemaker-studio) - Official AWS course (4 hours)
- [AWS SageMaker Practical (Udemy)](https://www.udemy.com/course/aws-machine-learning-a-complete-guide-with-python/) - Hands-on Python (10 hours)

#### NuGet Packages
```bash
dotnet add package AWSSDK.SageMaker
dotnet add package AWSSDK.SageMakerRuntime
```

#### Sample Implementation
```csharp
using Amazon.SageMakerRuntime;
using Amazon.SageMakerRuntime.Model;

public class SageMakerMlService
{
    private readonly IAmazonSageMakerRuntime _sageMakerClient;
    private readonly string _endpointName;
    
    public SageMakerMlService(IAmazonSageMakerRuntime sageMakerClient, IConfiguration config)
    {
        _sageMakerClient = sageMakerClient;
        _endpointName = config["SageMaker:EndpointName"];
    }
    
    public async Task<decimal> PredictFraudScoreAsync(Claim claim)
    {
        var input = new
        {
            instances = new[]
            {
                new
                {
                    amount = claim.TotalAmount,
                    documentCount = claim.Documents?.Count ?? 0,
                    claimantHistoryCount = 0,
                    daysSinceLastClaim = 0
                }
            }
        };
        
        var jsonInput = JsonSerializer.Serialize(input);
        
        var request = new InvokeEndpointRequest
        {
            EndpointName = _endpointName,
            ContentType = "application/json",
            Body = new MemoryStream(Encoding.UTF8.GetBytes(jsonInput))
        };
        
        var response = await _sageMakerClient.InvokeEndpointAsync(request);
        using var reader = new StreamReader(response.Body);
        var result = await reader.ReadToEndAsync();
        
        var prediction = JsonSerializer.Deserialize<SageMakerPrediction>(result);
        return (decimal)prediction.predictions[0][0];
    }
}

public class SageMakerPrediction
{
    public float[][] predictions { get; set; }
}
```

**Estimated Learning Time**: 12-15 hours

---

### 3. ML.NET on Azure (ML Inference)

#### Official Documentation
- [ML.NET Documentation](https://docs.microsoft.com/en-us/dotnet/machine-learning/) - Complete guide (3 hours)
- [ML.NET Tutorials](https://dotnet.microsoft.com/en-us/learn/ml-dotnet/get-started-tutorial/intro) - Interactive tutorials (2 hours)
- [Binary Classification Tutorial](https://docs.microsoft.com/en-us/dotnet/machine-learning/tutorials/sentiment-analysis) - Fraud detection pattern (1 hour)

#### Tools
- [ML.NET Model Builder](https://dotnet.microsoft.com/en-us/apps/machinelearning-ai/ml-dotnet/model-builder) - Visual Studio extension (30 min)
- [Deploy ML.NET to Azure](https://docs.microsoft.com/en-us/dotnet/machine-learning/how-to-guides/serve-model-web-api-ml-net) - Host models in App Service (1 hour)

#### Code Samples
- [ML.NET Samples Repository](https://github.com/dotnet/machinelearning-samples) - 100+ examples (3 hours)

#### Video Courses
- [ML.NET Series (YouTube)](https://www.youtube.com/playlist?list=PLdo4fOcmZ0oUFyIODTQqsNr6PFRGXpBQY) - 12-part series (3 hours)
- [Create ML Models with ML.NET](https://learn.microsoft.com/en-us/training/paths/create-machine-learn-models-with-ml-dotnet/) - Free certification path (6 hours)

#### Current Implementation
✅ **Already implemented in your project!** See `MlScoringService.cs` and `FraudModelTrainer.cs`

**Estimated Learning Time**: 8-10 hours (to deepen existing knowledge)

---

### 4. AWS Bedrock (Fraud AI)

#### Official Documentation
- [AWS Bedrock Documentation](https://docs.aws.amazon.com/bedrock/) - Platform overview (2 hours)
- [Bedrock User Guide](https://docs.aws.amazon.com/bedrock/latest/userguide/what-is-bedrock.html) - Complete guide (1 hour)
- [AWS SDK Bedrock Runtime](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/bedrock-runtime-examples.html) - C# examples (1 hour)

#### Claude 3 Specific
- [Anthropic Claude Documentation](https://docs.anthropic.com/claude/docs/intro-to-claude) - Claude-specific features (1 hour)
- [Prompt Engineering Guide](https://docs.anthropic.com/claude/docs/introduction-to-prompt-design) - Write better prompts (2 hours)

#### Hands-on Labs
- [Bedrock Workshop](https://catalog.us-east-1.prod.workshops.aws/workshops/a4bdb007-5600-4368-81c5-ff5b4154f518/en-US) - Interactive exercises (3 hours)
- [AWS Bedrock Introduction (YouTube)](https://www.youtube.com/watch?v=ab1mbj0acDo) - Overview video (30 min)

#### Code Samples
- [Bedrock .NET Examples](https://github.com/aws-samples/amazon-bedrock-samples) - Sample applications (2 hours)

#### NuGet Package
```bash
dotnet add package AWSSDK.BedrockRuntime
```

#### Sample Implementation
```csharp
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;

public class BedrockFraudAnalysisService
{
    private readonly IAmazonBedrockRuntime _bedrockClient;
    
    public BedrockFraudAnalysisService(IAmazonBedrockRuntime bedrockClient)
    {
        _bedrockClient = bedrockClient;
    }
    
    public async Task<(decimal FraudScore, string Reasoning)> AnalyzeClaimAsync(Claim claim)
    {
        var prompt = $@"You are a fraud detection expert. Analyze this insurance claim:

Policy ID: {claim.PolicyId}
Amount: ${claim.TotalAmount}
Documents: {claim.Documents?.Count ?? 0}
Submission Date: {claim.SubmittedDate}

Return a JSON response with:
1. fraudScore (0.0 to 1.0)
2. reasoning (brief explanation)
3. redFlags (array of suspicious indicators)

Format: {{""fraudScore"": 0.0, ""reasoning"": ""..."", ""redFlags"": []}}";

        var requestBody = new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = 1000,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };
        
        var request = new InvokeModelRequest
        {
            ModelId = "anthropic.claude-3-haiku-20240307-v1:0",
            ContentType = "application/json",
            Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(requestBody)))
        };
        
        var response = await _bedrockClient.InvokeModelAsync(request);
        
        using var reader = new StreamReader(response.Body);
        var responseJson = await reader.ReadToEndAsync();
        var result = JsonSerializer.Deserialize<BedrockResponse>(responseJson);
        
        var analysis = JsonSerializer.Deserialize<FraudAnalysis>(result.content[0].text);
        
        return (analysis.fraudScore, analysis.reasoning);
    }
}

public class BedrockResponse
{
    public Content[] content { get; set; }
}

public class Content
{
    public string text { get; set; }
}

public class FraudAnalysis
{
    public decimal fraudScore { get; set; }
    public string reasoning { get; set; }
    public string[] redFlags { get; set; }
}
```

**Estimated Learning Time**: 10-12 hours

---

### 5. AWS SES (Email)

#### Official Documentation
- [AWS SES Documentation](https://docs.aws.amazon.com/ses/) - Complete reference (2 hours)
- [SES Setup Guide](https://docs.aws.amazon.com/ses/latest/dg/send-email.html) - First email in 30 min (30 min)
- [AWS SDK for .NET - SES](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/ses-examples.html) - C# examples (1 hour)

#### Tutorials
- [Send Email with SES](https://aws.amazon.com/getting-started/hands-on/send-an-email/) - Step-by-step (20 min)
- [SES Email Templates](https://docs.aws.amazon.com/ses/latest/dg/send-personalized-email-api.html) - Reusable templates (1 hour)

#### Setup Guides
- [Verify Email/Domain](https://docs.aws.amazon.com/ses/latest/dg/verify-addresses-and-domains.html) - Required setup (15 min)
- [SES Best Practices](https://docs.aws.amazon.com/ses/latest/dg/best-practices.html) - Avoid spam filters (1 hour)

#### NuGet Package
```bash
dotnet add package AWSSDK.SimpleEmail
```

#### Sample Implementation
```csharp
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

public class SesNotificationService : INotificationService
{
    private readonly IAmazonSimpleEmailService _sesClient;
    private readonly ClaimsDbContext _context;
    
    public SesNotificationService(IAmazonSimpleEmailService sesClient, ClaimsDbContext context)
    {
        _sesClient = sesClient;
        _context = context;
    }
    
    public async Task SendEmailAsync(string recipientEmail, string subject, string body)
    {
        var request = new SendEmailRequest
        {
            Source = "noreply@claims.com",
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
        
        try
        {
            var response = await _sesClient.SendEmailAsync(request);
            Console.WriteLine($"Email sent! Message ID: {response.MessageId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email sending failed: {ex.Message}");
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
        
        await SendEmailAsync(recipientEmail, GetSubject(notificationType), notification.MessageBody);
        
        notification.Status = NotificationStatus.Sent;
        await _context.SaveChangesAsync();
    }
}
```

**Estimated Learning Time**: 5-6 hours

---

### 6. Azure SQL Database

#### Official Documentation
- [Azure SQL Documentation](https://docs.microsoft.com/en-us/azure/azure-sql/) - Complete platform guide (3 hours)
- [Create Azure SQL Database](https://docs.microsoft.com/en-us/azure/azure-sql/database/single-database-create-quickstart) - Portal + CLI (30 min)
- [EF Core with Azure SQL](https://docs.microsoft.com/en-us/ef/core/providers/sql-server/) - **Your current stack!** (1 hour)

#### Migration & Setup
- [Migrate to Azure SQL](https://docs.microsoft.com/en-us/azure/azure-sql/migration-guides/) - From on-prem SQL (2 hours)
- [Azure SQL Security](https://docs.microsoft.com/en-us/azure/azure-sql/database/security-overview) - Best practices (2 hours)
- [Performance Tuning](https://docs.microsoft.com/en-us/azure/azure-sql/database/performance-guidance) - Optimization tips (2 hours)

#### Learning Paths
- [Azure SQL Fundamentals](https://learn.microsoft.com/en-us/training/paths/azure-sql-fundamentals/) - Free certification (5 hours)
- [Azure SQL Deep Dive (YouTube)](https://www.youtube.com/playlist?list=PLlrxD0HtieHi5c9-i_Dnxw9vxBY-TqaeN) - Official series (4 hours)

#### Connection String Example
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:yourserver.database.windows.net,1433;Initial Catalog=ClaimsDB;Persist Security Info=False;User ID=sqladmin;Password=YourPassword123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

#### Migration Steps
1. Create Azure SQL Database (Portal or CLI)
2. Update connection string in `appsettings.json`
3. Run EF Core migrations:
```bash
dotnet ef database update --connection "YourAzureSQLConnectionString"
```

**Estimated Learning Time**: 6-8 hours

---

### 7. AWS S3 (Storage)

#### Official Documentation
- [AWS S3 Documentation](https://docs.aws.amazon.com/s3/) - Complete reference (3 hours)
- [S3 User Guide](https://docs.aws.amazon.com/AmazonS3/latest/userguide/Welcome.html) - Core concepts (1 hour)
- [AWS SDK for .NET - S3](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/s3-apis-intro.html) - C# examples (1 hour)

#### Tutorials
- [Upload Files to S3](https://aws.amazon.com/getting-started/hands-on/backup-files-to-amazon-s3/) - First upload (20 min)
- [Generate Pre-signed URLs](https://docs.aws.amazon.com/AmazonS3/latest/userguide/PresignedUrlUploadObject.html) - Secure direct uploads (1 hour)

#### Best Practices
- [S3 Best Practices](https://docs.aws.amazon.com/AmazonS3/latest/userguide/best-practices.html) - Performance & cost (1 hour)
- [S3 Security](https://docs.aws.amazon.com/AmazonS3/latest/userguide/security-best-practices.html) - Access control (2 hours)

#### NuGet Package
```bash
dotnet add package AWSSDK.S3
```

#### Sample Implementation
```csharp
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;

public class S3DocumentStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    
    public S3DocumentStorageService(IAmazonS3 s3Client, IConfiguration config)
    {
        _s3Client = s3Client;
        _bucketName = config["AWS:S3:BucketName"];
    }
    
    public async Task<string> UploadDocumentAsync(Stream fileStream, string fileName, string contentType)
    {
        var key = $"claims/{Guid.NewGuid()}/{fileName}";
        
        var transferUtility = new TransferUtility(_s3Client);
        await transferUtility.UploadAsync(fileStream, _bucketName, key);
        
        return key;
    }
    
    public async Task<string> GetPreSignedUrlAsync(string s3Key, int expirationMinutes = 60)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = s3Key,
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };
        
        return _s3Client.GetPreSignedURL(request);
    }
    
    public async Task<Stream> DownloadDocumentAsync(string s3Key)
    {
        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = s3Key
        };
        
        var response = await _s3Client.GetObjectAsync(request);
        return response.ResponseStream;
    }
}
```

**Estimated Learning Time**: 5-6 hours

---

### 8. Azure App Service (Hosting)

#### Official Documentation
- [Azure App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/) - Complete guide (3 hours)
- [Deploy ASP.NET Core to App Service](https://docs.microsoft.com/en-us/azure/app-service/quickstart-dotnetcore) - **Your exact scenario!** (30 min)
- [Deploy from Visual Studio](https://docs.microsoft.com/en-us/azure/app-service/quickstart-dotnetcore?pivots=development-environment-vs) - One-click deploy (20 min)

#### CI/CD
- [GitHub Actions for App Service](https://docs.microsoft.com/en-us/azure/app-service/deploy-github-actions) - Automated deployment (1 hour)
- [Azure DevOps Pipelines](https://docs.microsoft.com/en-us/azure/devops/pipelines/apps/cd/deploy-webdeploy-webapps) - Enterprise CI/CD (2 hours)

#### Configuration & Monitoring
- [App Settings & Connection Strings](https://docs.microsoft.com/en-us/azure/app-service/configure-common) - Environment variables (30 min)
- [Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core) - APM integration (1 hour)
- [Scale App Service](https://docs.microsoft.com/en-us/azure/app-service/manage-scale-up) - Auto-scaling (1 hour)

#### Learning Paths
- [Deploy .NET Apps to Azure](https://learn.microsoft.com/en-us/training/paths/deploy-dotnet-apps-azure/) - Free course (4 hours)
- [App Service Deployment (YouTube)](https://www.youtube.com/watch?v=4BwyqmRTrx8) - Full walkthrough (30 min)

#### Deployment Options

**Option 1: Visual Studio (Easiest)**
1. Right-click project → Publish
2. Select Azure → Azure App Service (Windows/Linux)
3. Create new or select existing App Service
4. Click Publish

**Option 2: Azure CLI**
```bash
# Login to Azure
az login

# Create resource group
az group create --name claims-rg --location eastus

# Create App Service plan
az appservice plan create --name claims-plan --resource-group claims-rg --sku B1

# Create web app
az webapp create --name claims-api-unique --resource-group claims-rg --plan claims-plan --runtime "DOTNET|8.0"

# Deploy code
az webapp up --name claims-api-unique --resource-group claims-rg --runtime "DOTNET|8.0"
```

**Option 3: GitHub Actions (CI/CD)**
```yaml
# .github/workflows/azure-deploy.yml
name: Deploy to Azure App Service

on:
  push:
    branches: [ main ]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '8.0.x'
    
    - name: Build
      run: dotnet build --configuration Release
    
    - name: Publish
      run: dotnet publish -c Release -o ./publish
    
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'claims-api-unique'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

**Estimated Learning Time**: 6-8 hours

---

## 🗓️ Complete Learning Path (10-Week Capstone)

| **Week** | **Focus** | **Components** | **Learning Resources** | **Hands-on Tasks** | **Hours** |
|----------|-----------|----------------|------------------------|-------------------|-----------|
| **Week 1** | Setup & Storage | AWS S3 + Azure SQL | S3 docs, Azure SQL fundamentals | Create S3 bucket, provision Azure SQL, migrate schema | 10 |
| **Week 2** | OCR Integration | AWS Textract | Textract workshop, .NET SDK guide | Replace Tesseract with Textract, test document extraction | 8 |
| **Week 3** | Email Service | AWS SES | SES tutorials, template guide | Replace MailKit with SES, create email templates | 6 |
| **Week 4-5** | ML Training | AWS SageMaker | SageMaker fraud example, AutoML guide | Train fraud model, deploy endpoint, test predictions | 15 |
| **Week 6-7** | AI Integration | AWS Bedrock | Bedrock workshop, prompt engineering | Integrate Claude 3, build fraud reasoning engine | 12 |
| **Week 8** | Deployment | Azure App Service | App Service deployment, CI/CD setup | Deploy API, configure environment, setup GitHub Actions | 8 |
| **Week 9** | Testing & Optimization | All components | Integration testing guides | End-to-end testing, performance tuning, load testing | 10 |
| **Week 10** | Documentation | Project delivery | Technical writing, presentation skills | Architecture docs, demo video, final presentation | 8 |

**Total Learning Time**: ~77 hours (spread over 10 weeks = ~8 hours/week)

---

## 🚀 Quick Start Guide

### Step 1: Install Required NuGet Packages

```bash
# AWS SDKs
dotnet add package AWSSDK.Textract
dotnet add package AWSSDK.SageMaker
dotnet add package AWSSDK.SageMakerRuntime
dotnet add package AWSSDK.BedrockRuntime
dotnet add package AWSSDK.SimpleEmail
dotnet add package AWSSDK.S3

# Azure SDKs (already installed)
dotnet add package Microsoft.Data.SqlClient
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

### Step 2: Configure AWS Credentials

```bash
# Install AWS CLI
# Windows: Download from https://aws.amazon.com/cli/
# Mac: brew install awscli
# Linux: sudo apt install awscli

# Configure credentials
aws configure
# Enter: Access Key ID
# Enter: Secret Access Key
# Enter: Default region (e.g., us-east-1)
# Enter: Output format (json)
```

### Step 3: Configure Azure CLI

```bash
# Install Azure CLI
# Windows: Download from https://aka.ms/installazurecliwindows
# Mac: brew install azure-cli
# Linux: curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Login to Azure
az login

# Set subscription
az account set --subscription "Your Subscription Name"
```

### Step 4: Update Configuration

Add to `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:yourserver.database.windows.net,1433;Database=ClaimsDB;User ID=admin;Password=xxx;Encrypt=True;"
  },
  "AWS": {
    "Region": "us-east-1",
    "S3": {
      "BucketName": "claims-documents-bucket"
    },
    "Textract": {
      "MaxPages": 10
    },
    "SageMaker": {
      "EndpointName": "fraud-detection-endpoint"
    },
    "Bedrock": {
      "ModelId": "anthropic.claude-3-haiku-20240307-v1:0"
    },
    "SES": {
      "SourceEmail": "noreply@yourdomain.com",
      "ReplyToEmail": "support@yourdomain.com"
    }
  },
  "Azure": {
    "ApplicationInsights": {
      "InstrumentationKey": "your-app-insights-key"
    }
  }
}
```

---

## 💰 Free Tier & Credits

### AWS Free Tier (12 months for new accounts)
- **AWS Textract**: 1,000 pages/month for 3 months
- **AWS SageMaker**: 250 hours of notebook usage for 2 months
- **AWS SES**: 62,000 emails/month (if sending from EC2)
- **AWS S3**: 5GB storage, 20,000 GET requests, 2,000 PUT requests
- **AWS Lambda**: 1M requests/month (always free)
- **New Account Credits**: $300 credit for first 12 months

### Azure Free Tier (12 months for new accounts)
- **Azure SQL Database**: 250GB storage
- **Azure App Service**: 10 web apps
- **Application Insights**: 5GB data ingestion
- **Azure Blob Storage**: 5GB locally redundant storage
- **New Account Credits**: $200 credit for first 30 days

**Pro Tip**: Use both AWS and Azure free tiers simultaneously for your capstone project! 🎓

---

## 📊 Cost Comparison

### Current POC (Free)
- Tesseract OCR: $0
- ML.NET: $0
- MailKit: $0
- In-Memory DB: $0
- **Total: $0/month**

### Capstone Production (5,000 claims/month)
- AWS Textract: $7.50
- AWS SageMaker Endpoint: $36 (ml.t3.medium 24/7)
- AWS Bedrock (optional): $120
- AWS SES: $0.50
- AWS S3: $1.15
- Azure SQL: $5
- Azure App Service: $13
- **Total: $167-183/month** (depending on AI usage)

---

## 📖 Additional Resources

### Community & Support
- [AWS Developer Forums](https://forums.aws.amazon.com/)
- [Azure Developer Community](https://techcommunity.microsoft.com/t5/azure-developer-community/ct-p/AzureDevCommunity)
- [Stack Overflow - AWS](https://stackoverflow.com/questions/tagged/amazon-web-services)
- [Stack Overflow - Azure](https://stackoverflow.com/questions/tagged/azure)
- [.NET Discord](https://discord.gg/dotnet)

### YouTube Channels
- [AWS Online Tech Talks](https://www.youtube.com/c/AWSOnlineTechTalks)
- [Microsoft Azure](https://www.youtube.com/c/MicrosoftAzure)
- [dotNET](https://www.youtube.com/c/dotNET)

### Blogs & Newsletters
- [AWS Architecture Blog](https://aws.amazon.com/blogs/architecture/)
- [Azure Architecture Center](https://docs.microsoft.com/en-us/azure/architecture/)
- [.NET Blog](https://devblogs.microsoft.com/dotnet/)

---

## 🎯 Success Metrics for Capstone

### Technical Deliverables
- ✅ OCR accuracy > 95% (AWS Textract)
- ✅ Fraud detection AUC > 0.85 (SageMaker)
- ✅ API response time < 2 seconds
- ✅ 99.9% uptime (Azure App Service)
- ✅ Automated CI/CD pipeline (GitHub Actions)

### Learning Outcomes
- ✅ Multi-cloud architecture design
- ✅ ML model deployment & monitoring
- ✅ Serverless and managed services integration
- ✅ Cloud cost optimization
- ✅ Production-ready .NET application

### Documentation
- ✅ Architecture diagrams (draw.io or Lucidchart)
- ✅ API documentation (Swagger)
- ✅ Deployment guide
- ✅ Cost analysis report
- ✅ Demo video (5-10 minutes)

---

## 📞 Getting Help

If you get stuck during your capstone implementation:

1. **Check the official documentation** (linked above for each service)
2. **Search Stack Overflow** with specific error messages
3. **Review GitHub sample repositories** for working code examples
4. **Use AWS/Azure support forums** for platform-specific questions
5. **Consult with your capstone advisor** for architecture decisions

---

## 🎓 Final Notes

This capstone project demonstrates real-world cloud architecture skills:

- **Multi-cloud integration** (AWS + Azure)
- **AI/ML in production** (OCR, fraud detection, generative AI)
- **Modern .NET development** (ASP.NET Core, EF Core, ML.NET)
- **DevOps practices** (CI/CD, infrastructure as code)
- **Cost-aware design** (free tiers, optimal service selection)

Good luck with your capstone! 🚀

---

**Last Updated**: December 27, 2025  
**Project**: Claims Validation System Capstone  
**Tech Stack**: .NET 8.0 + AWS + Azure
