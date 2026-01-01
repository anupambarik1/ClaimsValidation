# POC AI Integration Analysis - Free & Open Source Options

## Executive Summary

For a **working POC** with **zero licensing costs**, this analysis compares free AI tools across three critical components:
1. **OCR (Document Text Extraction)**
2. **ML Scoring (Fraud Detection)**
3. **Document Classification**
4. **Email Notifications**

---

## 🎯 Component-by-Component Analysis

### 1. OCR (Optical Character Recognition)

#### Option A: **Tesseract OCR** ⭐ RECOMMENDED
- **License**: Apache 2.0 (Free, Open Source)
- **Quality**: 85-95% accuracy for typed text, 60-80% for handwriting
- **Language Support**: 100+ languages
- **NuGet Package**: `Tesseract` (by Charles Weld)
- **Integration Complexity**: Low
- **Cost**: FREE

**Pros**:
- ✅ Completely free, no API limits
- ✅ Works offline (no external dependencies)
- ✅ Battle-tested (used by Google, Archive.org)
- ✅ C# wrapper available
- ✅ No Azure account needed

**Cons**:
- ❌ Lower accuracy than Azure Computer Vision (but acceptable for POC)
- ❌ Requires trained data files (~50MB download)

**Code Example**:
```csharp
using Tesseract;

public async Task<string> ExtractTextFromImage(string imagePath)
{
    using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
    using var img = Pix.LoadFromFile(imagePath);
    using var page = engine.Process(img);
    return page.GetText();
}
```

---

#### Option B: **PaddleOCR** (via Python interop)
- **License**: Apache 2.0
- **Quality**: 90-98% accuracy (better than Tesseract)
- **Integration**: Requires Python runtime + C# interop
- **Cost**: FREE

**Verdict**: More complex setup, but higher accuracy if you can handle Python dependencies.

---

#### Option C: **Azure Computer Vision Free Tier**
- **Cost**: FREE for first 5,000 transactions/month
- **Quality**: 95-99% accuracy
- **Limitations**: Requires Azure account, 20 calls/minute rate limit

**Verdict**: Best quality, but requires Azure setup. Good for POC if you already have Azure.

---

### 2. ML-Based Fraud Detection

#### Option A: **ML.NET** ⭐ RECOMMENDED
- **License**: MIT (Free, Open Source)
- **Provider**: Microsoft
- **Integration**: Native C# library
- **Cost**: FREE

**Capabilities**:
- Binary classification (Fraud vs. Legitimate)
- Anomaly detection
- Model training on your own data
- Pre-built algorithms (FastTree, LightGBM, etc.)

**Implementation Approach**:
```csharp
// 1. Train model on historical claims data
var mlContext = new MLContext();
var trainingData = mlContext.Data.LoadFromTextFile<ClaimData>("claims.csv");
var pipeline = mlContext.Transforms
    .Concatenate("Features", nameof(ClaimData.Amount), nameof(ClaimData.ClaimantHistory))
    .Append(mlContext.BinaryClassification.Trainers.FastTree());

var model = pipeline.Fit(trainingData);

// 2. Predict fraud score
var predictionEngine = mlContext.Model.CreatePredictionEngine<ClaimData, FraudPrediction>(model);
var prediction = predictionEngine.Predict(newClaim);
decimal fraudScore = prediction.Probability; // 0.0 - 1.0
```

**Pros**:
- ✅ No external API calls
- ✅ Runs locally, fast inference
- ✅ Can train custom models
- ✅ AutoML capabilities (automatically finds best algorithm)

**Cons**:
- ❌ Requires training data (but you can use synthetic data for POC)
- ❌ Initial model training takes time

---

#### Option B: **Google Gemini API Free Tier**
- **Cost**: FREE for 15 requests/minute (1,500/day)
- **Quality**: Excellent (LLM-based reasoning)
- **Use Case**: Send claim JSON → Get fraud assessment

**Example**:
```csharp
var prompt = $@"Analyze this insurance claim for fraud indicators:
Policy: {claim.PolicyId}, Amount: ${claim.Amount}, Documents: {claim.DocumentCount}
Return JSON with fraudScore (0-1) and reasons.";

var response = await geminiClient.GenerateContentAsync(prompt);
```

**Pros**:
- ✅ No training needed
- ✅ Sophisticated reasoning
- ✅ Free tier generous for POC

**Cons**:
- ❌ Requires internet connection
- ❌ Slower than local ML.NET model
- ❌ Rate limits

---

#### Option C: **Rule-Based Scoring** (Simplest)
- **Cost**: FREE
- **Implementation**: Hardcoded business rules

```csharp
decimal CalculateFraudScore(Claim claim)
{
    decimal score = 0.0m;
    
    // Rule 1: High amounts are suspicious
    if (claim.Amount > 5000) score += 0.3m;
    
    // Rule 2: Check claimant history
    if (GetClaimCount(claim.ClaimantId) > 3) score += 0.2m;
    
    // Rule 3: Incomplete documents
    if (claim.Documents.Count < 2) score += 0.25m;
    
    return Math.Min(score, 1.0m);
}
```

**Verdict**: Good for initial POC, but not "real AI".

---

### 3. Document Classification

#### Option A: **ML.NET Text Classification** ⭐ RECOMMENDED
- **License**: MIT (Free)
- **Approach**: Train on document text to classify type

```csharp
// Train classifier
var pipeline = mlContext.Transforms.Text
    .FeaturizeText("Features", nameof(Document.ExtractedText))
    .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy());

// Predict document type
var prediction = predictionEngine.Predict(new Document 
{ 
    ExtractedText = ocrText 
});
// Returns: "Invoice", "Receipt", "MedicalReport", etc.
```

---

#### Option B: **Keyword-Based Classification** (Simple)
```csharp
string ClassifyDocument(string text)
{
    if (text.Contains("invoice", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("bill to", StringComparison.OrdinalIgnoreCase))
        return "Invoice";
    
    if (text.Contains("receipt", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("total paid", StringComparison.OrdinalIgnoreCase))
        return "Receipt";
    
    return "Other";
}
```

---

#### Option C: **Google Gemini API**
- Use Gemini Flash (fast, free tier available)
- Send extracted text → Get document type classification

---

### 4. Email Notifications

#### Option A: **MailKit** ⭐ RECOMMENDED
- **License**: MIT (Free)
- **Provider**: Any SMTP server (Gmail, Outlook, etc.)
- **Cost**: FREE (use Gmail SMTP)

```csharp
using MailKit.Net.Smtp;
using MimeKit;

public async Task SendEmailAsync(string to, string subject, string body)
{
    var message = new MimeMessage();
    message.From.Add(new MailboxAddress("Claims System", "noreply@claims.com"));
    message.To.Add(new MailboxAddress("", to));
    message.Subject = subject;
    message.Body = new TextPart("html") { Text = body };

    using var client = new SmtpClient();
    await client.ConnectAsync("smtp.gmail.com", 587, false);
    await client.AuthenticateAsync("your-email@gmail.com", "app-password");
    await client.SendAsync(message);
    await client.DisconnectAsync(true);
}
```

**Gmail Setup** (Free):
1. Create Google account
2. Enable 2FA
3. Generate "App Password"
4. Use in SMTP config

**Pros**:
- ✅ Completely free
- ✅ No Azure dependency
- ✅ Reliable delivery

---

## 📊 Comparison Matrix

| Component | Azure (Paid) | Google Gemini | ML.NET | Tesseract | MailKit |
|-----------|-------------|---------------|---------|-----------|---------|
| **OCR** | ⭐⭐⭐⭐⭐ | N/A | N/A | ⭐⭐⭐⭐ | N/A |
| **Fraud Detection** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | N/A | N/A |
| **Classification** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | N/A | N/A |
| **Email** | ⭐⭐⭐⭐ | N/A | N/A | N/A | ⭐⭐⭐⭐⭐ |
| **Cost** | $$ | FREE* | FREE | FREE | FREE |
| **Setup Complexity** | Medium | Low | Medium | Low | Low |
| **Offline?** | ❌ | ❌ | ✅ | ✅ | ❌ |
| **Accuracy** | Highest | High | Medium-High | Medium | N/A |

*Free tier limits apply

---

## 🎯 RECOMMENDED POC ARCHITECTURE

### **Best Combination for Working POC (Zero Cost)**

```
1. OCR → Tesseract OCR (free, offline)
2. Fraud Detection → ML.NET (free, train custom model)
3. Document Classification → ML.NET Text Classification
4. Email → MailKit + Gmail SMTP (free)
```

**Total Cost**: **$0/month**  
**Internet Required**: Only for email  
**Azure Required**: No

---

### **Alternative: Hybrid Approach (Minimal Azure)**

```
1. OCR → Azure Computer Vision (5,000 free/month)
2. Fraud Detection → Google Gemini API (1,500 free/day)
3. Document Classification → ML.NET
4. Email → MailKit + Gmail SMTP
```

**Total Cost**: **$0/month** (within free tiers)  
**Better Accuracy**: Yes  
**Requires**: Azure account + Google API key

---

## 🛠️ Implementation Recommendations

### Phase 1: Pure Open Source (No Cloud Dependencies)
1. **Install NuGet Packages**:
   ```bash
   dotnet add package Tesseract --version 5.2.0
   dotnet add package Microsoft.ML --version 3.0.1
   dotnet add package MailKit --version 4.3.0
   ```

2. **Download Tesseract Trained Data**:
   - English: https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata
   - Place in `./tessdata/` folder

3. **Create Sample Training Data** (for ML.NET):
   - Generate synthetic claims CSV with fraud labels
   - Train fraud detection model
   - Save model file for inference

---

### Phase 2: Add Google Gemini (Optional Enhancement)
1. **Get Free API Key**: https://aistudio.google.com/app/apikey
2. **Install Package**:
   ```bash
   dotnet add package Google.Cloud.AIPlatform.V1
   ```
3. **Use for complex fraud reasoning**

---

### Phase 3: Azure Free Tier (If Better OCR Needed)
1. Create Azure free account
2. Create Computer Vision resource (Free tier: 5,000 calls/month)
3. Use for production-quality OCR

---

## 📦 Sample Data for ML.NET Training

Create `claims-training-data.csv`:
```csv
Amount,DocumentCount,ClaimantHistoryCount,DaysSinceLastClaim,IsFraud
500.00,2,0,0,0
1500.00,3,1,120,0
5000.00,1,5,10,1
750.00,2,2,90,0
10000.00,1,8,5,1
```

Train model:
```csharp
var data = mlContext.Data.LoadFromTextFile<ClaimTrainingData>(
    "claims-training-data.csv", 
    hasHeader: true, 
    separatorChar: ',');

var pipeline = mlContext.Transforms
    .Concatenate("Features", 
        nameof(ClaimTrainingData.Amount),
        nameof(ClaimTrainingData.DocumentCount),
        nameof(ClaimTrainingData.ClaimantHistoryCount),
        nameof(ClaimTrainingData.DaysSinceLastClaim))
    .Append(mlContext.BinaryClassification.Trainers.FastTree(labelColumnName: "IsFraud"));

var model = pipeline.Fit(data);
mlContext.Model.Save(model, data.Schema, "fraud-model.zip");
```

---

## 🎯 Final Recommendation: **Tesseract + ML.NET + MailKit**

### Why This Combination?
1. ✅ **Zero Cost**: No subscriptions, no API limits
2. ✅ **Fully Functional**: Real OCR, real ML, real emails
3. ✅ **Offline Capable**: Works without internet (except email)
4. ✅ **Production-Ready**: These are enterprise-grade libraries
5. ✅ **C# Native**: No Python interop, no cross-language complexity
6. ✅ **Easy Demo**: Run locally, no cloud account signup needed

### Accuracy Expectations
- **OCR**: 85-90% (good enough for typed documents)
- **Fraud Detection**: 70-85% (with proper training data)
- **Email Delivery**: 99%+

### POC Limitations to Communicate
- OCR accuracy lower than commercial solutions (but free)
- ML model needs training data (use synthetic for demo)
- Gmail SMTP has daily send limits (500/day for free accounts)

---

## 📋 Next Steps

1. **Approve architecture choice**
2. **Install recommended packages**
3. **Create sample training data**
4. **Implement Tesseract OCR service**
5. **Train ML.NET fraud model**
6. **Configure Gmail SMTP**
7. **Test end-to-end flow**

---

**Document Version**: 1.0  
**Analysis Date**: December 11, 2025  
**Recommendation**: Tesseract + ML.NET + MailKit (Pure Open Source Stack)
