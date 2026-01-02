# 📦 Complete Testing Package - Ready to Use

## ✅ What You Now Have

### 1. Sample Document (Ready to Use)
📄 **File**: `sample-claim-document.txt` (126 lines, ~3KB)

**Contains Complete Insurance Claim**:
- Claim #CLM-2024-001234
- Policyholder: John Michael Davis
- Type: Auto Accident (2023 Honda Accord)
- Total Amount: $8,850.00
- Date: January 15, 2024
- Location: Springfield, IL
- Includes: Witness statements, damage details, medical info, supporting docs

**Perfect for testing because it has**:
- ✅ Rich, detailed narrative
- ✅ Multiple entities (names, dates, amounts, locations)
- ✅ Clear structure for OCR
- ✅ Legitimate claim (low fraud indicators)
- ✅ Complete documentation trail

---

### 2. Testing Tools

#### Automated Test Script
📜 **File**: `test-nlp-api.ps1`

**One command to test everything**:
```powershell
.\TestDocuments\test-nlp-api.ps1
```

**What it does**:
1. ✅ Submits a claim
2. ✅ Adds the sample document
3. ✅ Processes the claim (triggers NLP)
4. ✅ Displays all results with color-coded output
5. ✅ Shows NLP analysis, fraud scores, decision

**No manual steps required!**

---

#### Manual Testing via Swagger
🌐 **Browser**: http://localhost:5000/swagger

**Steps**:
1. Start API: `dotnet run`
2. Open Swagger URL
3. POST `/api/claims/submit-and-process`
4. Use sample claim JSON
5. View results including NLP analysis

---

### 3. Documentation

#### Quick Reference (1 page)
📋 **File**: `QUICK_REFERENCE.md`
- Copy/paste commands
- What to look for in response
- Common issues & fixes
- Success indicators

#### Complete Testing Guide
📚 **File**: `TESTING_GUIDE.md`
- Detailed API endpoints
- Test payloads & responses
- Expected NLP results
- What each component tests

#### Full README
📖 **File**: `README.md`
- Overview of all files
- Quick start options
- Expected output examples
- Testing scenarios
- Troubleshooting guide

---

## 🚀 Getting Started (Choose One Path)

### Path A: Automated (Fastest - 30 seconds)
```powershell
# Terminal 1: Start API
cd src/Claims.Api
dotnet run

# Terminal 2: Run test script (wait for API to start)
cd c:\Hackathon Projects
.\TestDocuments\test-nlp-api.ps1
```

**Output**: Color-coded results showing:
- ✅ NLP Summary
- ✅ Fraud Risk Score (NLP)
- ✅ Combined Fraud Score
- ✅ Final Decision
- ✅ Processing Time

---

### Path B: Manual via Swagger (2 minutes)
1. Start API: `cd src/Claims.Api && dotnet run`
2. Open: http://localhost:5000/swagger
3. Find: `POST /api/claims/submit-and-process`
4. Paste sample JSON
5. Click Execute
6. Scroll to `nlpAnalysis` section

---

### Path C: Step-by-Step Testing (5 minutes)
1. Submit claim → `POST /api/claims`
2. Add document → `POST /api/claims/{claimId}/documents`
3. Process → `POST /api/claims/{claimId}/process`
4. Review results

---

## 📊 Sample Output Example

```json
{
  "finalDecision": "ManualReview",
  "decisionReason": "Requires manual review: Moderate risk...",
  
  "nlpAnalysis": {
    "summary": "Auto accident claim for John Davis. Vehicle collision 
               at Main and Oak intersection. Total damages $8,850. 
               Two witnesses present.",
    "fraudRiskScore": 0.28,
    "claimType": "auto",
    "detectedEntities": "{\"names\": [\"John Michael Davis\", 
                        \"Robert James Thompson\", ...], 
                        \"dates\": [\"January 15, 2024\", ...],
                        \"amounts\": [8850.00, ...],
                        \"claimType\": \"auto\"}"
  },
  
  "mlScoring": {
    "fraudScore": 0.38,          ← Combined (60% ML + 40% NLP)
    "approvalScore": 0.65,
    "fraudRiskLevel": "Low"
  },
  
  "processingTimeMs": 2847.5
}
```

---

## ✅ Verification Checklist

After running the test, verify:

- [ ] Script completes without errors
- [ ] Response includes `nlpAnalysis` object
- [ ] Fraud scores are between 0.0 and 1.0
- [ ] Summary is 2-3 sentences
- [ ] Detected entities include names, dates, amounts
- [ ] `claimType` is correctly identified as "auto"
- [ ] `mlScoring.fraudScore` shows combined calculation
- [ ] Final decision is made (AutoApprove/Reject/ManualReview)
- [ ] Processing time is logged (< 5 seconds expected)

**If all checked ✅ = Your NLP integration is working!**

---

## 📁 Complete File Structure

```
TestDocuments/
├── README.md                        ← Start here
├── QUICK_REFERENCE.md              ← Quick commands & troubleshooting
├── TESTING_GUIDE.md                ← Detailed API testing guide
│
├── sample-claim-document.txt        ← USE THIS (main test document)
│
├── test-nlp-api.ps1                ← RUN THIS (automated testing)
│
├── TestDocumentGenerator.cs         ← Create custom documents (C#)
├── create-sample-document.ps1       ← Create documents (PowerShell)
└── create-sample-claim-pdf.ps1      ← Convert to PDF (PowerShell)
```

---

## 🎯 What Gets Tested

When you run the automated test, the NLP pipeline processes:

1. **OCR Extraction** (from document text)
   - Extracts text with confidence score
   - Classifies document type

2. **NLP Bedrock Summarization** ← NEW!
   - Summarizes claim in 2-3 sentences
   - Uses Claude 3 Haiku model

3. **Fraud Analysis** ← NEW!
   - Bedrock analyzes narrative for fraud indicators
   - Comprehend analyzes sentiment
   - Combines both for fraud risk score

4. **Entity Extraction** ← NEW!
   - Extracts names, dates, amounts, locations
   - Classifies claim type (auto, medical, property, etc.)

5. **Fraud Score Combination** ← NEW!
   - Traditional ML fraud score: 60% weight
   - NLP fraud score: 40% weight
   - Combined score drives final decision

6. **Decision Making**
   - Auto-approve (low risk)
   - Reject (high risk)
   - Manual review (moderate risk)

---

## 🔄 Testing Variations

### Test 1: Default (Included)
- Document: `sample-claim-document.txt`
- Expected: ManualReview
- Reason: Legitimate claim, needs human verification

### Test 2: High-Risk Claim (Create Yourself)
- Vague descriptions
- Missing information
- Inconsistencies
- Expected: Reject

### Test 3: Low-Risk Claim (Create Yourself)
- Clear, detailed narrative
- Complete information
- Professional documentation
- Expected: AutoApprove

---

## 🛠️ Customization

### Create New Test Documents

```powershell
# Option 1: Use generator script
.\TestDocuments\create-sample-document.ps1

# Option 2: Edit sample document
notepad .\TestDocuments\sample-claim-document.txt

# Option 3: Use C# generator
dotnet run TestDocuments\TestDocumentGenerator.cs
```

---

## 📞 Support

### Quick Help
- See: `QUICK_REFERENCE.md` (common issues)
- See: `TESTING_GUIDE.md` (detailed guide)

### Common Issues

| Problem | Solution |
|---------|----------|
| "Port 5000 in use" | Change port in `launchSettings.json` |
| "AWS credentials error" | Add credentials to `appsettings.json` |
| "NLP results missing" | Check logs for "Step 2.5: NLP Analysis" |
| "Script won't run" | Run: `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned` |
| "File not found" | Verify path: `c:\Hackathon Projects\TestDocuments\` |

---

## 🎓 Learning Resources

1. **Quick Start**: QUICK_REFERENCE.md (1 page)
2. **Complete Guide**: TESTING_GUIDE.md (detailed)
3. **Full README**: README.md (comprehensive)
4. **Implementation Plan**: AWS_NLP_EXECUTION_PLAN.md (architecture)

---

## ✨ Summary

You have everything needed to:

✅ Test the NLP integration immediately
✅ Verify fraud score calculation works
✅ Confirm Bedrock + Comprehend integration
✅ Validate end-to-end claim processing
✅ Create custom test documents
✅ Monitor performance

**No additional setup required!**

---

## 🚀 Next Action

### Fastest Path (30 seconds):
```powershell
# Terminal 1
cd src/Claims.Api && dotnet run

# Terminal 2 (wait 30 seconds for API to start)
cd c:\Hackathon Projects
.\TestDocuments\test-nlp-api.ps1
```

**Result**: See full NLP analysis and fraud scoring in action! ✅

---

**You're ready to test! Start with the automated script and follow the output.**
