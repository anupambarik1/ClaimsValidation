# 🧪 Detailed NLP Backend Testing Steps

**Complete Step-by-Step Guide for Testing AWS NLP Integration**

---

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Option A: Automated Testing (Fastest)](#option-a-automated-testing)
3. [Option B: Manual Testing via Swagger](#option-b-manual-testing-via-swagger)
4. [Option C: Step-by-Step Testing](#option-c-step-by-step-testing)
5. [Understanding the Results](#understanding-the-results)
6. [Verification Checklist](#verification-checklist)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

Before you start testing, verify these are in place:

### 1. AWS Configuration
**File**: `src/Claims.Api/appsettings.json`

Check these settings are configured:
```json
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
  }
}
```

**If AWS credentials are missing**: 
- The system will gracefully fall back to default values
- NLP features will be disabled
- You'll see "AWS NLP service is disabled" in logs

### 2. Sample Document
**File**: `TestDocuments/sample-claim-document.txt`

✅ Verify it exists (126 lines, ~3KB)

Contains:
- Complete insurance claim
- Policyholder: John Michael Davis
- Claim amount: $8,850.00
- Auto accident claim with details

### 3. Solution Builds
Run this to verify no compilation errors:
```powershell
cd "c:\Hackathon Projects"
dotnet build ClaimsValidation.sln
```

Expected output:
```
Build succeeded in X.Xs
```

### 4. Ports Available
- Port 5000: Default API port (or configured port)
- Verify it's not in use: `netstat -ano | findstr :5000`

---

## Option A: Automated Testing (Fastest)

**Time: 30-60 seconds | Complexity: Minimal**

This is the quickest way to test everything end-to-end.

### Step 1: Start the API Server

**Terminal 1 - Start API**
```powershell
cd "c:\Hackathon Projects\src\Claims.Api"
dotnet run
```

Expected output:
```
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to exit.
```

⏳ **Wait until you see "Now listening on"** before proceeding.

### Step 2: Run the Automated Test Script

**Terminal 2 - Run Test**
```powershell
cd "c:\Hackathon Projects"
.\TestDocuments\test-nlp-api.ps1
```

If you get an execution policy error:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

Then run the script again.

### Step 3: View the Results

The script will output:

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
  Summary: Auto accident claim for John Davis. Vehicle collision at Main 
          and Oak intersection on Jan 15, 2024. Total damages $8,850 
          after deductible. Two witnesses present.
  Fraud Risk Score (NLP): 0.28
  Claim Type: auto
  Detected Entities: {"names": ["John Michael Davis", "Robert James Thompson"...

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

Next Steps:
1. Check application logs for NLP processing steps
2. Verify fraud scores are in valid range (0.0-1.0)
3. Review the NLP summary for accuracy
4. Confirm final decision is appropriate for risk level
```

### Step 4: Verify Success

Check that output includes:
- ✅ Claim submitted successfully
- ✅ Document added successfully
- ✅ Claim processed successfully
- ✅ NLP ANALYSIS RESULTS section
- ✅ All fraud scores between 0.0-1.0
- ✅ Processing time < 5000 ms

**If you see all of the above: NLP is working! ✅**

---

## Option B: Manual Testing via Swagger

**Time: 2-3 minutes | Complexity: Low**

Interactive testing through Swagger UI.

### Step 1: Start the API

Same as Option A Step 1:
```powershell
cd "c:\Hackathon Projects\src\Claims.Api"
dotnet run
```

Wait for: "Now listening on: http://localhost:5000"

### Step 2: Open Swagger UI

Open your browser and navigate to:
```
http://localhost:5000/swagger
```

You should see:
```
Swagger UI
│
├─ Claims
│  ├─ GET /api/claims/user/{claimantId}
│  ├─ POST /api/claims
│  ├─ GET /api/claims/{claimId}/status
│  ├─ PUT /api/claims/{claimId}/status
│  ├─ POST /api/claims/{claimId}/documents
│  ├─ POST /api/claims/{claimId}/process
│  └─ POST /api/claims/submit-and-process  ← USE THIS ONE
│
└─ Status
   └─ GET /api/status
```

### Step 3: Submit and Process Claim

Find the endpoint: **`POST /api/claims/submit-and-process`**

Click: **"Try it out"**

### Step 4: Enter Test Data

In the request body, paste:
```json
{
  "policyId": "POL-2024-567890",
  "claimantId": "CLMT-JOHN-DAVIS",
  "totalAmount": 8850.00
}
```

### Step 5: Execute Request

Click: **"Execute"**

### Step 6: View Response

Scroll down to see the response. Look for:

**Success Response (200 OK)**:
```json
{
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "success": true,
  "finalDecision": "ManualReview",
  "decisionReason": "Requires manual review: Moderate risk...",
  
  "nlpAnalysis": {
    "summary": "Auto accident claim...",
    "fraudRiskScore": 0.28,
    "detectedEntities": "{...}",
    "claimType": "auto"
  },
  
  "mlScoring": {
    "fraudScore": 0.38,
    "approvalScore": 0.65,
    "fraudRiskLevel": "Low"
  },
  
  "ocrResults": [
    {
      "documentId": "...",
      "success": true,
      "extractedText": "CLAIM INFORMATION...",
      "confidence": 0.95,
      "classifiedType": "AccidentReport"
    }
  ],
  
  "processingTimeMs": 2847.5
}
```

### Step 7: Verify Results

Check response includes:
- ✅ `nlpAnalysis` object with summary
- ✅ Fraud scores between 0.0-1.0
- ✅ `claimType` identified
- ✅ `finalDecision` made
- ✅ Processing time logged

**If present: NLP is working! ✅**

---

## Option C: Step-by-Step Testing

**Time: 5-10 minutes | Complexity: Medium**

Test each step individually for detailed control.

### Step 1: Start the API

```powershell
cd "c:\Hackathon Projects\src\Claims.Api"
dotnet run
```

Wait for: "Now listening on"

### Step 2: Submit a Claim

**Using Swagger**:
1. Find: `POST /api/claims`
2. Click: "Try it out"
3. Enter:
```json
{
  "policyId": "POL-2024-567890",
  "claimantId": "CLMT-JOHN-DAVIS",
  "totalAmount": 8850.00
}
```
4. Click: "Execute"

**Expected Response**:
```json
{
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Submitted",
  "submittedDate": "2024-01-20T10:00:00Z",
  "message": "Claim submitted successfully"
}
```

✅ **Save the claimId** for next steps.

### Step 3: Add Document to Claim

**Using Swagger**:
1. Find: `POST /api/claims/{claimId}/documents`
2. Click: "Try it out"
3. Replace `{claimId}` with your saved claim ID (e.g., `550e8400-e29b-41d4-a716-446655440000`)
4. Enter body:
```json
{
  "filePath": "c:\\Hackathon Projects\\TestDocuments\\sample-claim-document.txt",
  "documentType": "AccidentReport"
}
```
5. Click: "Execute"

**Expected Response**:
```json
{
  "documentId": "660f8500-f39c-51e5-b817-557766551111",
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "documentType": "AccidentReport",
  "uploadedDate": "2024-01-20T10:05:00Z",
  "message": "Document added successfully"
}
```

✅ **Document is now attached to claim**

### Step 4: Process Claim (Triggers NLP)

**Using Swagger**:
1. Find: `POST /api/claims/{claimId}/process`
2. Click: "Try it out"
3. Replace `{claimId}` with your claim ID
4. No request body needed (leave blank)
5. Click: "Execute"

⏳ **Wait 5-10 seconds** while processing happens

**Processing Steps in API**:
```
Step 1: OCR Processing (text extraction)
Step 2: Document Classification
Step 2.5: NLP Analysis ✨ NEW! ✨
  - Bedrock Summarization
  - Fraud Analysis (Bedrock + Comprehend)
  - Entity Extraction
Step 3: ML Fraud Detection
Step 4: Fraud Score Combination (60% ML + 40% NLP)
Step 5: Final Decision
```

**Expected Response** (200 OK):
```json
{
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "success": true,
  "finalDecision": "ManualReview",
  "decisionReason": "Requires manual review: Moderate risk...",
  
  "ocrResults": [
    {
      "documentId": "660f8500-f39c-51e5-b817-557766551111",
      "success": true,
      "extractedText": "CLAIM INFORMATION\n...",
      "confidence": 0.95,
      "classifiedType": "AccidentReport"
    }
  ],
  
  "rulesValidation": {
    "isValid": true,
    "reason": "All business rules passed",
    "rulesChecked": ["PolicyLimit", "PolicyValidity", "CoverageCheck"],
    "ruleResults": {...}
  },
  
  "nlpAnalysis": {
    "summary": "Auto accident claim for John Davis. Vehicle collision at Main and Oak intersection on Jan 15, 2024. Total damages $8,850 after deductible. Two witnesses present.",
    "fraudRiskScore": 0.28,
    "detectedEntities": "{\"names\": [\"John Michael Davis\", \"Robert James Thompson\", \"Sarah Michelle Johnson\", \"Robert Chen\"], \"dates\": [\"January 15, 2024\", \"January 20, 2024\"], \"locations\": [\"Springfield, IL\"], \"amounts\": [8850.00, 8500.00, 450.00], \"claimType\": \"auto\"}",
    "claimType": "auto"
  },
  
  "mlScoring": {
    "fraudScore": 0.38,
    "approvalScore": 0.65,
    "fraudRiskLevel": "Low",
    "riskFactors": [...]
  },
  
  "processingTimeMs": 2847.5
}
```

✅ **NLP analysis complete!**

---

## Understanding the Results

### NLP Analysis Object

**What it contains**:

#### Summary (Generated by Bedrock)
```json
"summary": "Auto accident claim for John Davis. Vehicle collision at Main 
           and Oak intersection on Jan 15, 2024. Total damages $8,850 
           after deductible. Two witnesses present."
```
- Generated by Claude 3 Haiku (Bedrock)
- Should be 2-3 sentences
- Captures key points of the claim
- ✅ Indicates Bedrock is working

#### Fraud Risk Score (NLP)
```json
"fraudRiskScore": 0.28
```
- Range: 0.0 (safe) to 1.0 (definitely fraud)
- Generated by Bedrock + Comprehend
- Lower scores = less fraud risk
- For this sample: 0.28 = Low risk (legitimate claim)
- ✅ Indicates fraud analysis is working

#### Detected Entities (Extracted)
```json
"detectedEntities": "{\"names\": [\"John Michael Davis\", ...], 
                    \"dates\": [\"January 15, 2024\", ...], 
                    \"locations\": [\"Springfield, IL\"], 
                    \"amounts\": [8850.00, ...], 
                    \"claimType\": \"auto\"}"
```
- Extracted by Comprehend
- Contains: names, dates, locations, amounts
- Claim type: auto/medical/property/life
- ✅ Indicates entity extraction working

#### Claim Type Classification (Bedrock)
```json
"claimType": "auto"
```
- Classified by Bedrock Claude 3
- For sample: "auto" (vehicle accident)
- ✅ Indicates classification working

### ML Scoring Results

#### Combined Fraud Score (60% ML + 40% NLP)
```json
"fraudScore": 0.38
```

**Calculation**:
```
ML Fraud Score: 0.45 (from traditional ML model)
NLP Fraud Score: 0.28 (from Bedrock + Comprehend)

Combined = (0.45 × 0.6) + (0.28 × 0.4)
         = 0.27 + 0.112
         = 0.382 (rounded to 0.38)
```

#### Approval Score
```json
"approvalScore": 0.65
```
- Probability of approval (0.0-1.0)
- Higher is better
- Used with fraud score for decision

#### Fraud Risk Level
```json
"fraudRiskLevel": "Low"
```
- Low: < 0.3
- Medium: 0.3 - 0.7
- High: > 0.7

For combined score 0.38: Medium risk technically, but still reasonable for approval.

### Final Decision

#### Decision
```json
"finalDecision": "ManualReview"
```

**Possible values**:
- `"AutoApprove"` - Low fraud risk (< 0.3) + high approval score (> 0.8)
- `"Reject"` - High fraud risk (> 0.7)
- `"ManualReview"` - Moderate risk (0.3-0.7) or uncertain conditions

For sample: ManualReview is appropriate (0.38 fraud score = moderate)

#### Decision Reason
```json
"decisionReason": "Requires manual review: Moderate risk 
                  (Fraud: 0.38, Approval: 0.65)"
```
- Human-readable explanation
- Shows which factors influenced decision

---

## Verification Checklist

After testing, verify all items are present and correct:

### Response Structure
- [ ] Response status code is 200 (success)
- [ ] `"success": true` is present
- [ ] `"claimId"` is populated with valid GUID

### NLP Analysis
- [ ] `"nlpAnalysis"` object exists (not null)
- [ ] `"summary"` is readable text (2-3 sentences)
- [ ] `"fraudRiskScore"` is number between 0.0 and 1.0
- [ ] `"detectedEntities"` contains JSON string with data
- [ ] `"claimType"` is set to "auto" (for this sample)

### ML Scoring
- [ ] `"mlScoring"` object exists
- [ ] `"fraudScore"` is between 0.0 and 1.0 (combined score)
- [ ] `"approvalScore"` is between 0.0 and 1.0
- [ ] `"fraudRiskLevel"` is "Low", "Medium", or "High"

### OCR Results
- [ ] `"ocrResults"` array has at least 1 item
- [ ] `"success": true` for document processing
- [ ] `"extractedText"` contains extracted text
- [ ] `"confidence"` is between 0.0 and 1.0 (should be > 0.9)

### Decision
- [ ] `"finalDecision"` is one of: AutoApprove, Reject, ManualReview
- [ ] `"decisionReason"` provides explanation

### Performance
- [ ] `"processingTimeMs"` is logged (should be < 5000)

**If all checkmarks are checked: ✅ NLP integration is working!**

---

## Checking Application Logs

While the test runs, check the API terminal for these log messages:

### Expected Log Sequence

```
info: Claims.Services.Implementations.ClaimsService[0]
      Starting AI processing for claim {ClaimId}

info: Claims.Services.Implementations.ClaimsService[0]
      Step 1: OCR Processing for 1 documents

info: Claims.Services.Implementations.ClaimsService[0]
      Document {DocumentId} processed: Type=AccidentReport, Confidence=95%

info: Claims.Services.Implementations.ClaimsService[0]
      Step 2: Business Rules Validation

info: Claims.Services.Implementations.ClaimsService[0]
      Step 2.5: NLP Analysis          ← NEW NLP STEP!

info: Claims.Services.Aws.AWSNlpService[0]
      Claim summarized successfully

info: Claims.Services.Aws.AWSNlpService[0]
      Fraud analysis completed. Risk: Low

info: Claims.Services.Aws.AWSNlpService[0]
      Entities extracted: 4 names, 2 dates

info: Claims.Services.Implementations.ClaimsService[0]
      NLP Analysis completed: Fraud Risk Score=28.00%

info: Claims.Services.Implementations.ClaimsService[0]
      Step 3: ML Fraud Detection and Scoring

info: Claims.Services.Implementations.ClaimsService[0]
      Claim {ClaimId} scored: FraudScore=38.00% 
      (ML=45.00% + NLP=28.00%), ApprovalScore=65.00%

info: Claims.Services.Implementations.ClaimsService[0]
      Step 5: Determine final decision

info: Claims.Services.Implementations.ClaimsService[0]
      Claim {ClaimId} routed to manual review

info: Claims.Services.Implementations.ClaimsService[0]
      Claim {ClaimId} processing completed: ManualReview
```

**Key indicators NLP is working**:
- ✅ "Step 2.5: NLP Analysis" appears
- ✅ "Claim summarized successfully" appears
- ✅ "Fraud analysis completed" appears
- ✅ "Entities extracted" appears
- ✅ Combined fraud score is calculated

---

## Troubleshooting

### Issue 1: "nlpAnalysis is null" in Response

**Symptom**:
```json
"nlpAnalysis": null
```

**Causes**:
1. AWS not enabled in appsettings.json
2. Bedrock service not registered
3. NLP service not properly configured

**Solution**:
```json
// In appsettings.json
"AWS": {
  "Enabled": true,          ← Set to true
  "AccessKey": "YOUR_KEY",  ← Add credentials
  "SecretKey": "YOUR_SECRET",
  "Bedrock": {
    "Enabled": true         ← Set to true
  }
}
```

### Issue 2: Fraud Scores are 0.0

**Symptom**:
```json
"fraudRiskScore": 0.0,
"fraudScore": 0.0
```

**Cause**: AWS NLP service disabled or not responding

**Solution**:
1. Check AWS:Enabled = true
2. Check AWS credentials are valid
3. Check internet connectivity
4. Check Bedrock model is available in your region

### Issue 3: "Port 5000 in use" Error

**Symptom**:
```
System.IO.IOException: Unable to bind to http://localhost:5000
```

**Solution**:
```powershell
# Option 1: Kill the process using port 5000
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# Option 2: Use a different port
# Edit: src/Claims.Api/Properties/launchSettings.json
# Change "applicationUrl": "http://localhost:5001"
```

### Issue 4: Script Execution Policy Error

**Symptom**:
```
File cannot be loaded because running scripts is disabled on this system
```

**Solution**:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
# Answer: Y (yes)

# Then run script again
.\TestDocuments\test-nlp-api.ps1
```

### Issue 5: Document File Not Found

**Symptom**:
```
Error adding document: File not found at c:\...
```

**Solution**:
1. Verify file exists: 
   ```powershell
   Test-Path "c:\Hackathon Projects\TestDocuments\sample-claim-document.txt"
   ```
2. Use correct path with double backslashes:
   ```json
   "filePath": "c:\\Hackathon Projects\\TestDocuments\\sample-claim-document.txt"
   ```

### Issue 6: Processing Timeout

**Symptom**:
```
Request timeout after 30 seconds
```

**Cause**: 
- Bedrock API slow
- Large document
- Network latency

**Solution**:
1. Wait and retry (temporary network issue)
2. Reduce document size
3. Increase request timeout in API configuration

### Issue 7: "AWS credentials not configured"

**Symptom**:
```
AmazonServiceException: Unable to locate credentials
```

**Solution**:
```json
// Ensure credentials in appsettings.json
"AWS": {
  "AccessKey": "AKIAXXXXXXXXXXX",  ← Your AWS access key
  "SecretKey": "XXXXXXXXXXX"         ← Your AWS secret key
}
```

Or use AWS credential file:
```
~/.aws/credentials
```

---

## Quick Reference Commands

### Start API
```powershell
cd "c:\Hackathon Projects\src\Claims.Api"
dotnet run
```

### Run Automated Test
```powershell
cd "c:\Hackathon Projects"
.\TestDocuments\test-nlp-api.ps1
```

### Open Swagger
```
http://localhost:5000/swagger
```

### Build Solution
```powershell
cd "c:\Hackathon Projects"
dotnet build ClaimsValidation.sln
```

### Check Logs
```powershell
# In API terminal, watch for these:
# "Step 2.5: NLP Analysis"
# "Claim summarized successfully"
# "Fraud analysis completed"
```

### Edit Sample Document
```powershell
notepad "c:\Hackathon Projects\TestDocuments\sample-claim-document.txt"
```

---

## Success Summary

✅ **All of these should be true after testing**:

1. API starts without errors
2. Test script runs to completion
3. Response includes `nlpAnalysis` object
4. All fraud scores between 0.0-1.0
5. Summary is readable and relevant
6. Entities extracted correctly
7. Claim type identified correctly
8. Final decision made appropriately
9. Processing time logged (< 5 seconds)
10. Logs show "Step 2.5: NLP Analysis"

**If all 10 are true: NLP Integration is working correctly! 🎉**

---

## Next Steps After Successful Testing

1. **Create Custom Claims**: Modify sample document for different scenarios
2. **Test Different Cases**: High-risk, low-risk, borderline claims
3. **Monitor Performance**: Track processing times
4. **Verify Accuracy**: Check if fraud detection is reasonable
5. **Integrate with UI**: Build frontend once backend verified
6. **Deploy to Production**: Configure AWS credentials for live environment

---

**Now you have everything documented and ready to test! Start with Option A (Automated Testing) for fastest results.**
