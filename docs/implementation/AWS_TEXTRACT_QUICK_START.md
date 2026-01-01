# AWS Textract Implementation - Quick Start Guide

**Time to Setup:** 15 minutes  
**Time to Test:** 10 minutes  
**Total:** 25 minutes

---

## ⚡ 5-Minute Overview

You now have **AWS Textract Document Intelligence** integrated into your Claims Validation System.

**What it does:**
- Extracts text from documents (PDF, images)
- Detects and parses tables
- Extracts form fields (key-value pairs)
- Classifies document type automatically
- Returns confidence scores for all data

**Supported formats:** PDF, PNG, JPG, TIFF, WEBP

---

## 🚀 Setup (15 minutes)

### Step 1: Install NuGet Packages (3 min)

**Option A: Via Package Manager Console**
```powershell
Install-Package AWSSDK.Textract
Install-Package AWSSDK.S3
```

**Option B: Via .NET CLI**
```bash
cd src/Claims.Services
dotnet add package AWSSDK.Textract
dotnet add package AWSSDK.S3
```

**Option C: Edit Claims.Services.csproj**
```xml
<ItemGroup>
    <PackageReference Include="AWSSDK.Textract" Version="3.7.100.0" />
    <PackageReference Include="AWSSDK.S3" Version="3.7.100.0" />
    <PackageReference Include="AWSSDK.Core" Version="3.7.100.0" />
</ItemGroup>
```

### Step 2: Configure AWS Credentials (3 min)

**Option A: Environment Variables** (Easiest for Development)
```powershell
$env:AWS_ACCESS_KEY_ID = "YOUR_ACCESS_KEY"
$env:AWS_SECRET_ACCESS_KEY = "YOUR_SECRET_KEY"
$env:AWS_DEFAULT_REGION = "us-east-1"
```

**Option B: AWS Credentials File** (Recommended)
Location: `~/.aws/credentials`
```ini
[default]
aws_access_key_id = YOUR_ACCESS_KEY
aws_secret_access_key = YOUR_SECRET_KEY
```

**Option C: appsettings.json** (Not recommended for production)
```json
{
  "AWS": {
    "Enabled": true,
    "Region": "us-east-1",
    "AccessKey": "YOUR_ACCESS_KEY",
    "SecretKey": "YOUR_SECRET_KEY"
  }
}
```

### Step 3: Configure appsettings.json (5 min)

Add to your `appsettings.Development.json`:

```json
{
  "AWS": {
    "Enabled": true,
    "Region": "us-east-1",
    "S3BucketName": "claims-documents",
    "Textract": {
      "Enabled": true
    }
  }
}
```

### Step 4: Create S3 Bucket (4 min)

**Via AWS CLI:**
```bash
aws s3 mb s3://claims-documents --region us-east-1
```

**Via AWS Console:**
1. Go to https://s3.console.aws.amazon.com
2. Click "Create bucket"
3. Name: `claims-documents`
4. Region: `us-east-1`
5. Click "Create"

---

## 🧪 Testing (10 minutes)

### Test 1: Simple Text Extraction (5 min)

Create a test file `Test_Textract.cs`:

```csharp
using Claims.Services.Aws;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Claims.Infrastructure.Data;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.Development.json")
    .AddEnvironmentVariables()
    .Build();

var logger = LoggerFactory.Create(builder => builder.AddConsole())
    .CreateLogger<AWSTextractService>();

var context = new ClaimsDbContext(); // Your context

var service = new AWSTextractService(config, logger, context);

// Test with S3 document
var (text, confidence) = await service.ProcessDocumentAsync("s3://claims-documents/test-invoice.pdf");

Console.WriteLine($"Extracted Text:\n{text}");
Console.WriteLine($"Confidence: {confidence:P}");
```

Run it:
```bash
dotnet run --project src/Claims.Api
```

### Test 2: Advanced Intelligence (5 min)

```csharp
using Claims.Services.Aws;

var intelligenceService = new AWSTextractDocumentIntelligenceService(config, logger, context);

var result = await intelligenceService.AnalyzeDocumentWithStructureAsync("s3://claims-documents/invoice.pdf");

Console.WriteLine($"Document Type: {result.DocumentType}");
Console.WriteLine($"Text Confidence: {result.TextConfidence:P}");
Console.WriteLine($"Tables Found: {result.Tables.Count}");
Console.WriteLine($"Form Fields: {result.FormFields.Count}");

// Print extracted tables
foreach (var table in result.Tables)
{
    Console.WriteLine($"\nTable {table.TableId}:");
    foreach (var row in table.Rows)
    {
        Console.WriteLine(string.Join(" | ", row));
    }
}

// Print form fields
foreach (var field in result.FormFields)
{
    Console.WriteLine($"{field.Key}: {field.Value.Value} ({field.Value.Confidence:P})");
}
```

---

## 📝 Upload a Test Document

### Using AWS CLI:
```bash
aws s3 cp my-invoice.pdf s3://claims-documents/test-invoice.pdf
```

### Using AWS Console:
1. Go to S3 bucket: https://s3.console.aws.amazon.com
2. Find `claims-documents` bucket
3. Click "Upload"
4. Select your PDF/image
5. Click "Upload"

### Using .NET Code:
```csharp
var blobService = new AWSBlobStorageService(config, logger);
var s3Uri = await blobService.UploadAsync(file, "test-invoice.pdf");
Console.WriteLine($"Uploaded to: {s3Uri}");
```

---

## 📊 API Integration

### Add to ClaimsController:

```csharp
[HttpPost("analyze-document")]
public async Task<IActionResult> AnalyzeDocument(string s3Uri)
{
    var service = HttpContext.RequestServices.GetRequiredService<AWSTextractDocumentIntelligenceService>();
    var result = await service.AnalyzeDocumentWithStructureAsync(s3Uri);
    
    if (!result.Success)
        return BadRequest(result.Error);
    
    return Ok(result);
}
```

### Call it:
```bash
POST /api/claims/analyze-document?s3Uri=s3://claims-documents/invoice.pdf

Response:
{
  "success": true,
  "extractedText": "Invoice #12345...",
  "textConfidence": 0.92,
  "documentType": "Invoice",
  "tables": [...],
  "formFields": {...}
}
```

---

## ✅ Verification Checklist

- [ ] NuGet packages installed
- [ ] AWS credentials configured
- [ ] S3 bucket created
- [ ] appsettings.json updated
- [ ] Code compiles without errors
- [ ] Test document uploaded to S3
- [ ] Text extraction test passed
- [ ] Document classification working
- [ ] Table extraction working
- [ ] Form fields extracted correctly
- [ ] Confidence scores present
- [ ] Ready for production!

---

## 🎯 Common Test Documents

### Invoice PDF
```
Content:
- Invoice #12345
- Date: 2024-01-01
- Items table with qty/price
- Subtotal, Tax, Total
```

### Receipt
```
Content:
- Receipt printed format
- Transaction details
- Total amount
- Thank you message
```

### Claim Form
```
Content:
- Form fields (text boxes)
- Claim #, Date, Claimant Name
- Description of loss
- Signature line
```

### Bank Statement
```
Content:
- Bank name and account
- Transaction table
- Balance information
- Period dates
```

---

## 📞 Troubleshooting

### "InvalidSignatureException"
**Problem:** Wrong AWS credentials  
**Solution:** Verify AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY are correct

### "NoSuchBucket"
**Problem:** S3 bucket doesn't exist  
**Solution:** Run: `aws s3 mb s3://claims-documents --region us-east-1`

### "AccessDenied"
**Problem:** AWS user lacks permissions  
**Solution:** Ensure user has Textract and S3 permissions (see IAM policy below)

### "InvalidS3Uri"
**Problem:** Wrong S3 URI format  
**Solution:** Use format: `s3://bucket-name/key` (not `http://` or `https://`)

---

## 🔐 IAM Permissions Required

Minimal policy for AWS user:

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "textract:AnalyzeDocument",
                "textract:AnalyzeExpenseDocument"
            ],
            "Resource": "*"
        },
        {
            "Effect": "Allow",
            "Action": [
                "s3:GetObject",
                "s3:PutObject",
                "s3:ListBucket"
            ],
            "Resource": [
                "arn:aws:s3:::claims-documents",
                "arn:aws:s3:::claims-documents/*"
            ]
        }
    ]
}
```

---

## 💰 Costs

During testing (first 1000 pages free in many cases):
- **0 - 1000 pages:** $0 (often free tier)
- **1001 - 10000 pages:** $1
- **10001+ pages:** Varies

For MVP (100 docs/month, ~200 pages):
- Monthly cost: ~$0.20
- Annual cost: ~$2.40

---

## 📚 Full Documentation

See: `docs/implementation/AWS_TEXTRACT_DOCUMENT_INTELLIGENCE.md`

Topics:
- Complete API reference
- Advanced usage examples
- Performance optimization
- Security best practices
- Integration patterns
- Testing strategies

---

## 🎓 What's Next?

After testing Document Intelligence:

1. **Integrate with Claims API** - Connect to document upload flow
2. **Add NLP with Bedrock** - Implement chatbot for claim understanding
3. **Set up RDS Database** - Replace in-memory with persistent DB
4. **Implement Blob Storage** - Full S3 integration
5. **Deploy to Production** - Go live!

---

## 📞 Support

If you encounter issues:

1. Check troubleshooting section above
2. Review full documentation: `docs/implementation/AWS_TEXTRACT_DOCUMENT_INTELLIGENCE.md`
3. Check AWS Textract docs: https://docs.aws.amazon.com/textract/
4. Verify IAM permissions

---

**Ready to Process Documents!** 🚀

Test it now with:
```powershell
dotnet run --project src/Claims.Api
```
