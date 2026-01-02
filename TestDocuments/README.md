# Sample Claims Documents - Quick Start Guide

## 📁 Files Available

### Testing Documents

| File | Purpose |
|------|---------|
| `sample-claim-document.txt` | ✅ **USE THIS** - Complete sample auto claim for testing |
| `TESTING_GUIDE.md` | Detailed guide on how to test the NLP API |
| `test-nlp-api.ps1` | Automated PowerShell script for end-to-end testing |

### Generator Scripts (for creating custom documents)

| File | Purpose |
|------|---------|
| `TestDocumentGenerator.cs` | C# utility to generate sample documents |
| `create-sample-document.ps1` | PowerShell script to generate documents |
| `create-sample-claim-pdf.ps1` | PowerShell script to create PDF versions |

---

## 🚀 Quick Start (2 Minutes)

### Option A: Automatic Testing (Easiest)

```powershell
cd "C:\Hackathon Projects"
.\TestDocuments\test-nlp-api.ps1
```

This will automatically:
1. ✅ Submit a claim
2. ✅ Add the sample document
3. ✅ Process the claim (triggers NLP)
4. ✅ Display all results with color-coded output

### Option B: Manual Testing with Swagger

1. Start the API:
   ```powershell
   cd src/Claims.Api
   dotnet run
   ```

2. Open Swagger: http://localhost:5000/swagger

3. Use this endpoint: `POST /api/claims/submit-and-process`

4. Submit this JSON:
   ```json
   {
     "policyId": "POL-2024-567890",
     "claimantId": "CLMT-JOHN-DAVIS",
     "totalAmount": 8850.00
   }
   ```

5. Response will include:
   - ✅ NLP Summary
   - ✅ Fraud Risk Score (NLP)
   - ✅ Extracted Entities
   - ✅ Combined Fraud Score (60% ML + 40% NLP)
   - ✅ Final Decision

---

## 📄 Sample Document Details

**File**: `sample-claim-document.txt`

**Contains**:
- ✅ Complete claim metadata
- ✅ Policyholder information
- ✅ Detailed accident narrative (2,000+ words)
- ✅ Injury descriptions
- ✅ Damage assessment with costs
- ✅ Witness information (2 witnesses)
- ✅ Supporting documentation list
- ✅ Other driver information
- ✅ Professional declaration

**Claim Details**:
- Policy: POL-2024-567890
- Claimant: John Michael Davis
- Type: Auto Accident
- Total Claim: $8,850.00
- Location: Springfield, IL
- Date: January 15, 2024

---

## 🧪 What Gets Tested

The sample document tests all NLP components:

### 1. OCR Processing
```
Input: sample-claim-document.txt
Output: Extracted text with 95%+ confidence
```

### 2. NLP Bedrock Summarization
```
Input: Full claim narrative
Output: "Auto accident claim for John Davis. Vehicle collision at Main 
        and Oak intersection on Jan 15, 2024. Total damages $8,850 
        after deductible. Two witnesses present."
```

### 3. Fraud Analysis (Bedrock + Comprehend)
```
Input: Claim narrative
Output: Fraud Risk Score 0.28 (Low risk)
Reason: Clear narrative, professional documentation, witnesses, police report
```

### 4. Entity Extraction (Comprehend + Bedrock)
```
Output:
{
  "names": ["John Michael Davis", "Robert James Thompson", ...],
  "dates": ["January 15, 2024", "January 20, 2024"],
  "amounts": [8850.00, 8500.00, 450.00],
  "locations": ["Springfield, IL"],
  "claimType": "auto"
}
```

### 5. Combined Fraud Scoring
```
ML Fraud Score: 0.45
NLP Fraud Score: 0.28
Combined Score: (0.45 × 0.6) + (0.28 × 0.4) = 0.382

Range: 0.0 (no fraud) to 1.0 (certain fraud)
```

### 6. Decision Making
```
Final Decision: ManualReview
Reason: Legitimate claim with moderate risk level
        Recommend human review for final approval
```

---

## 📊 Expected Output Example

When you run the test, you should see something like:

```
======================================
Claims API - NLP Integration Test
======================================

[STEP 1] Submitting a new claim...
✓ Claim submitted successfully
  Claim ID: 550e8400-e29b-41d4-a716-446655440000

[STEP 2] Adding sample document to claim...
✓ Document added successfully
  Document ID: 660f8500-f39c-51e5-b817-557766551111
  File: AccidentReport

[STEP 3] Processing claim (NLP Analysis in progress)...
  - OCR text extraction
  - NLP Bedrock summarization
  - Fraud analysis (Bedrock + Comprehend)
  - Entity extraction
  - ML scoring + NLP combination

✓ Claim processed successfully

======================================
RESULTS
======================================

Final Decision: ManualReview
Decision Reason: Requires manual review: Moderate risk (Fraud: 0.38, Approval: 0.65)

NLP ANALYSIS RESULTS:
  Summary: Auto accident claim for John Davis...
  Fraud Risk Score (NLP): 0.28
  Claim Type: auto
  Detected Entities: {"names": ["John Michael Davis"...

ML SCORING RESULTS:
  Fraud Score (Combined 60% ML + 40% NLP): 0.38
  Approval Score: 0.65
  Fraud Risk Level: Low

OCR RESULTS:
  Document Type: AccidentReport
  Confidence: 95.0%
  Text Extracted: CLAIM INFORMATION Claim Number: CLM-2024-0...

PERFORMANCE:
  Total Processing Time: 2847.5 ms

======================================
Test Complete!
======================================
```

---

## ✅ Testing Checklist

- [ ] Start the API server
- [ ] Run `test-nlp-api.ps1` OR manually test via Swagger
- [ ] Verify NLP Summary is generated
- [ ] Check Fraud Risk Score is between 0.0-1.0
- [ ] Confirm Combined Fraud Score is calculated (60% ML + 40% NLP)
- [ ] Verify Final Decision is made (AutoApprove/Reject/ManualReview)
- [ ] Check logs for "Step 2.5: NLP Analysis" message
- [ ] Review processing time (should be < 5 seconds)

---

## 🔄 Testing Different Scenarios

### Test Case 1: Valid, Low-Risk Claim
**Document**: Use `sample-claim-document.txt` as-is
**Expected**: `AutoApprove` (low fraud risk, high approval score)

### Test Case 2: High-Risk Claim
**Create document with**:
- Vague descriptions
- Missing information
- Inconsistencies
**Expected**: `Reject` (high fraud risk)

### Test Case 3: Medium-Risk Claim  
**Create document with**:
- Some details missing
- Borderline information
**Expected**: `ManualReview`

---

## 🛠️ Troubleshooting

### Error: "Document not found"
```
Solution: Ensure sample-claim-document.txt exists in TestDocuments folder
```

### Error: "AWS credentials not configured"
```
Solution: Add AWS credentials to appsettings.json
AWS:Enabled: true
AWS:AccessKey: YOUR_KEY
AWS:SecretKey: YOUR_SECRET
```

### Error: "Bedrock not enabled"
```
Solution: Set AWS:Bedrock:Enabled to true in appsettings.json
```

### No NLP results in response
```
Solution: Check application logs for "Step 2.5: NLP Analysis"
          If missing, verify AWS:Enabled and AWS:Bedrock:Enabled are true
```

---

## 📚 Next Steps

1. **Run the automated test**: `.\test-nlp-api.ps1`
2. **Review the results**: Check NLP analysis output
3. **Check the logs**: Look for NLP processing steps
4. **Create custom documents**: Modify the sample for different scenarios
5. **Integrate with UI**: Connect to frontend once NLP is verified

---

**Happy Testing! 🚀**
