# Quick Reference - Testing NLP Integration

## 📋 One-Page Cheat Sheet

### Files Location
```
c:\Hackathon Projects\TestDocuments\
├── sample-claim-document.txt    ← USE THIS FOR TESTING
├── test-nlp-api.ps1           ← RUN THIS SCRIPT
├── README.md                    ← Full guide
├── TESTING_GUIDE.md            ← API testing guide
└── [other files]
```

---

## 🚀 Fastest Way to Test (Copy & Paste)

### 1. Start the API
```powershell
cd "C:\Hackathon Projects\src\Claims.Api"
dotnet run
```

Wait for: "Now listening on: http://localhost:5000"

### 2. Open New PowerShell Terminal
```powershell
cd "C:\Hackathon Projects"
.\TestDocuments\test-nlp-api.ps1
```

That's it! ✅

---

## 🌐 Or Test via Swagger UI

1. Open: http://localhost:5000/swagger
2. Find: `POST /api/claims/submit-and-process`
3. Click: "Try it out"
4. Paste:
```json
{
  "policyId": "POL-2024-567890",
  "claimantId": "CLMT-JOHN-DAVIS",
  "totalAmount": 8850.00
}
```
5. Click: "Execute"
6. Scroll down to see: `nlpAnalysis` section ✅

---

## ✅ What to Look for in Response

```json
{
  "nlpAnalysis": {
    "summary": "Auto accident claim for John Davis...",      ✅ Bedrock
    "fraudRiskScore": 0.28,                                 ✅ Fraud Score
    "detectedEntities": "{...}",                            ✅ Entities
    "claimType": "auto"                                     ✅ Classification
  },
  
  "mlScoring": {
    "fraudScore": 0.38,                    ✅ Combined (60% ML + 40% NLP)
    "approvalScore": 0.65,
    "fraudRiskLevel": "Low"
  },
  
  "finalDecision": "ManualReview"          ✅ Decision
}
```

---

## 📊 Key Metrics

| Metric | Expected Range | Status |
|--------|-----------------|--------|
| Fraud Risk Score (NLP) | 0.0 - 1.0 | ✅ 0.28 |
| Combined Fraud Score | 0.0 - 1.0 | ✅ 0.38 |
| Processing Time | < 5000 ms | ✅ ~2800 ms |
| Approval Score | 0.0 - 1.0 | ✅ 0.65 |

---

## 🔍 How to Verify It's Working

### In Response JSON:
- ✅ `nlpAnalysis` object exists
- ✅ `fraudRiskScore` is between 0.0 and 1.0
- ✅ `summary` is 2-3 sentences
- ✅ `detectedEntities` contains names/dates/amounts
- ✅ `claimType` is "auto"
- ✅ `mlScoring.fraudScore` is combined score

### In Application Logs:
Search for these messages:
- "Step 1: OCR Processing"
- **"Step 2.5: NLP Analysis"** ← New!
- "Claim summarized successfully" ← Bedrock
- "Fraud analysis completed" ← Bedrock + Comprehend
- "Entities extracted"
- "Step 3: ML Fraud Detection"

---

## 🎯 Testing Sequence

```
1️⃣  Start API (dotnet run)
    ↓
2️⃣  Run Test Script (.\test-nlp-api.ps1)
    ↓
3️⃣  Check Response for NLP results
    ↓
4️⃣  Verify fraud scores (0.0-1.0 range)
    ↓
5️⃣  Check logs for "Step 2.5: NLP Analysis"
    ↓
✅  Success!
```

---

## ⚙️ Configuration Required

**File**: `src/Claims.Api/appsettings.json`

```json
{
  "AWS": {
    "Enabled": true,
    "AccessKey": "YOUR_KEY",
    "SecretKey": "YOUR_SECRET",
    "Bedrock": {
      "Enabled": true,
      "Model": "anthropic.claude-3-5-haiku-20241022-v1:0"
    }
  }
}
```

---

## 🐛 Common Issues

| Issue | Solution |
|-------|----------|
| "nlpAnalysis is null" | Check AWS:Enabled = true in appsettings.json |
| "fraudScore is 0" | Check Bedrock credentials |
| "Script won't run" | Run: `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser` |
| "Port 5000 in use" | Change port in `Properties/launchSettings.json` |
| "File not found" | Verify: `c:\Hackathon Projects\TestDocuments\sample-claim-document.txt` exists |

---

## 📞 What Each Component Does

| Component | Input | Output |
|-----------|-------|--------|
| **Bedrock** | Claim narrative | Summary + Fraud score |
| **Comprehend** | Text | Sentiment + Entities |
| **AWSBedrockService** | Prompt | Claude 3 response |
| **AWSNlpService** | Claim text | All NLP results combined |
| **ClaimsService** | NLP + ML scores | Combined fraud score (60/40) |

---

## 🎓 Learning Path

```
1. Run test script ✅
   └─ See NLP results in action

2. Read TESTING_GUIDE.md
   └─ Understand each step

3. Check application logs
   └─ See NLP processing steps

4. Modify sample document
   └─ Test different scenarios

5. Create custom claims
   └─ Build confidence
```

---

## 📈 Success Indicators

✅ All of these should be true:

- [ ] API starts without errors
- [ ] Script runs to completion
- [ ] Response includes `nlpAnalysis` object
- [ ] Fraud scores are 0.0-1.0 range
- [ ] Summary is readable and relevant
- [ ] Decision is made (AutoApprove/Reject/ManualReview)
- [ ] Processing time is logged
- [ ] No errors in application logs

**If all ✅ = NLP Integration is Working!**

---

## 🚀 Next Steps

After successful testing:

1. **Create variations** of the sample document
2. **Test different claim types** (medical, property, life, etc.)
3. **Benchmark performance** with larger documents
4. **Integrate with frontend** UI
5. **Monitor fraud detection** accuracy in production

---

**Happy Testing! 🎉**
