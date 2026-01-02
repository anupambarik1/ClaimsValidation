# 📋 Request Payloads for Option B (Swagger Testing)

**Complete JSON payloads ready to copy-paste into Swagger UI**

---

## Overview

Option B requires 3 API calls in sequence:
1. **Submit Claim** - `POST /api/claims`
2. **Add Document** - `POST /api/claims/{claimId}/documents`
3. **Process Claim** - `POST /api/claims/{claimId}/process`

Each section below shows the exact JSON to paste into Swagger.

---

## Step 1: Submit a Claim

**Endpoint**: `POST /api/claims`

**In Swagger**:
1. Find: `POST /api/claims` (under Claims section)
2. Click: "Try it out"
3. Copy the JSON below into the request body field
4. Click: "Execute"

### Request Payload

```json
{
  "policyId": "POL-2024-567890",
  "claimantId": "CLMT-JOHN-DAVIS",
  "totalAmount": 8850.00
}
```

**Field Descriptions**:
- `policyId` - Insurance policy number (format: POL-YYYY-XXXXXX)
- `claimantId` - Unique identifier for person filing claim (format: CLMT-FIRST-LAST)
- `totalAmount` - Total claim amount in dollars (decimal, e.g., 8850.00)

### Expected Response (200 OK)

```json
{
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Submitted",
  "submittedDate": "2024-01-20T10:00:00Z",
  "message": "Claim submitted successfully"
}
```

### ✅ Save This Value

**Copy the `claimId` from response**:
```
550e8400-e29b-41d4-a716-446655440000
```

You'll need this for Step 2 and Step 3.

---

## Step 2: Add Document to Claim

**Endpoint**: `POST /api/claims/{claimId}/documents`

**In Swagger**:
1. Find: `POST /api/claims/{claimId}/documents` (under Claims section)
2. Click: "Try it out"
3. Replace `{claimId}` parameter with your saved claim ID from Step 1
   - Example: `550e8400-e29b-41d4-a716-446655440000`
4. Copy the JSON below into the request body
5. Click: "Execute"

### URL Parameter

**Replace** `{claimId}` **with**:
```
550e8400-e29b-41d4-a716-446655440000
```

(Use the value you got from Step 1)

### Request Payload

```json
{
  "filePath": "c:\\Hackathon Projects\\TestDocuments\\sample-claim-document.txt",
  "documentType": "AccidentReport"
}
```

**Field Descriptions**:
- `filePath` - Full path to document (use double backslashes: `\\`)
  - Must point to: `sample-claim-document.txt` in TestDocuments folder
  - Important: Use `\\` not single `\`
- `documentType` - Type of document: `AccidentReport`, `MedicalReport`, `PropertyDamage`, etc.
  - For this test: use `AccidentReport`

### Expected Response (200 OK)

```json
{
  "documentId": "660f8500-f39c-51e5-b817-557766551111",
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "documentType": "AccidentReport",
  "uploadedDate": "2024-01-20T10:05:00Z",
  "message": "Document added successfully"
}
```

### ✅ Save This Value (Optional)

**Copy the `documentId`** (not needed for next step, but useful for reference):
```
660f8500-f39c-51e5-b817-557766551111
```

---

## Step 3: Process Claim (Triggers NLP)

**Endpoint**: `POST /api/claims/{claimId}/process`

**In Swagger**:
1. Find: `POST /api/claims/{claimId}/process` (under Claims section)
2. Click: "Try it out"
3. Replace `{claimId}` parameter with your saved claim ID from Step 1
   - Example: `550e8400-e29b-41d4-a716-446655440000`
4. **Leave request body empty** (no JSON needed)
5. Click: "Execute"
6. ⏳ Wait 5-10 seconds for processing

### URL Parameter

**Replace** `{claimId}` **with**:
```
550e8400-e29b-41d4-a716-446655440000
```

(Use the value you got from Step 1)

### Request Body

**Leave EMPTY** - No request body needed for this endpoint.

Just click Execute with no body.

### Expected Response (200 OK)

```json
{
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "success": true,
  "finalDecision": "ManualReview",
  "decisionReason": "Requires manual review: Moderate risk (Fraud: 0.38, Approval: 0.65)",
  
  "ocrResults": [
    {
      "documentId": "660f8500-f39c-51e5-b817-557766551111",
      "success": true,
      "extractedText": "CLAIM INFORMATION\nClaim Number: CLM-2024-001234\nPolicy Number: POL-2024-567890\nClaimant Name: John Michael Davis\nClaim Date: January 15, 2024\nClaim Amount: $8,850.00\n...",
      "confidence": 0.95,
      "classifiedType": "AccidentReport"
    }
  ],
  
  "rulesValidation": {
    "isValid": true,
    "reason": "All business rules passed",
    "rulesChecked": [
      "PolicyLimit",
      "PolicyValidity",
      "CoverageCheck"
    ]
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
    "riskFactors": [
      {
        "factor": "SuspiciousPatterns",
        "weight": 0.25,
        "score": 0.6
      }
    ]
  },
  
  "processingTimeMs": 2847.5
}
```

### ✅ Verify NLP Results

In the response, check these NLP-specific fields:

**nlpAnalysis object**:
- ✅ `"summary"`: 2-3 sentence summary of claim
- ✅ `"fraudRiskScore"`: Number between 0.0-1.0 (0.28 in example)
- ✅ `"detectedEntities"`: JSON string with names, dates, locations, amounts
- ✅ `"claimType"`: "auto" (or medical, property, life)

**mlScoring object**:
- ✅ `"fraudScore"`: Combined score (0.38 in example) = (ML × 0.6) + (NLP × 0.4)
- ✅ `"finalDecision"`: AutoApprove, Reject, or ManualReview

---

## Alternative: Single Request (submit-and-process)

If you prefer, there's a combined endpoint that does steps 1-3 in one call:

**Endpoint**: `POST /api/claims/submit-and-process`

**In Swagger**:
1. Find: `POST /api/claims/submit-and-process`
2. Click: "Try it out"
3. Copy this payload:

```json
{
  "policyId": "POL-2024-567890",
  "claimantId": "CLMT-JOHN-DAVIS",
  "totalAmount": 8850.00
}
```

4. Click: "Execute"
5. Get same response as Step 3 above

This does everything in one call - useful if you don't need to see intermediate results.

---

## Quick Reference

| Step | Endpoint | Method | URL Param | Body | Needs ClaimId |
|------|----------|--------|-----------|------|-----------------|
| 1 | `/api/claims` | POST | None | ✅ Yes | - |
| 2 | `/api/claims/{claimId}/documents` | POST | ✅ Yes | ✅ Yes | From Step 1 |
| 3 | `/api/claims/{claimId}/process` | POST | ✅ Yes | No | From Step 1 |

---

## Troubleshooting Payloads

### Issue: "Invalid claimId format"

**If you get this error**, the claimId might not be in UUID format.

**Solution**: Make sure you copied the full claimId from Step 1 response:
```
550e8400-e29b-41d4-a716-446655440000
```
(Should have dashes in correct positions)

### Issue: "Document file not found"

**If you get this error**, the file path is incorrect.

**Correct payload**:
```json
{
  "filePath": "c:\\Hackathon Projects\\TestDocuments\\sample-claim-document.txt",
  "documentType": "AccidentReport"
}
```

**Key points**:
- Use `\\` (double backslash) not `\` (single)
- Filename must be exactly: `sample-claim-document.txt`
- Path must include full directory structure

### Issue: "404 Not Found"

**If endpoint not found**, check:
1. API is running: `dotnet run` in Claims.Api folder
2. Swagger URL is correct: `http://localhost:5000/swagger`
3. Endpoint name is exact match from Swagger UI

---

## Full Test Sequence with Values

Here's a complete walk-through with actual values you'll use:

### Step 1 Request
```json
{
  "policyId": "POL-2024-567890",
  "claimantId": "CLMT-JOHN-DAVIS",
  "totalAmount": 8850.00
}
```

**Response includes** (example):
```json
{
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Submitted"
}
```

↓ Copy claimId ↓

### Step 2 Request
URL: `POST /api/claims/550e8400-e29b-41d4-a716-446655440000/documents`

```json
{
  "filePath": "c:\\Hackathon Projects\\TestDocuments\\sample-claim-document.txt",
  "documentType": "AccidentReport"
}
```

**Response includes** (example):
```json
{
  "documentId": "660f8500-f39c-51e5-b817-557766551111",
  "claimId": "550e8400-e29b-41d4-a716-446655440000"
}
```

↓ Use same claimId ↓

### Step 3 Request
URL: `POST /api/claims/550e8400-e29b-41d4-a716-446655440000/process`

**Body**: Empty (no JSON)

**Response includes** (example):
```json
{
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "success": true,
  "finalDecision": "ManualReview",
  "nlpAnalysis": {
    "summary": "Auto accident claim...",
    "fraudRiskScore": 0.28,
    "claimType": "auto"
  },
  "mlScoring": {
    "fraudScore": 0.38
  }
}
```

---

## Copy-Paste Helper

**Step 1 - Copy this entire block**:
```
{
  "policyId": "POL-2024-567890",
  "claimantId": "CLMT-JOHN-DAVIS",
  "totalAmount": 8850.00
}
```

**Step 2 - Copy this entire block**:
```
{
  "filePath": "c:\\Hackathon Projects\\TestDocuments\\sample-claim-document.txt",
  "documentType": "AccidentReport"
}
```

**Step 3 - Leave empty, click Execute**

---

## Success Indicators

After all 3 steps, you should see:

✅ Step 1: Claim created with `claimId`
✅ Step 2: Document added successfully
✅ Step 3: Response includes `nlpAnalysis` object
✅ Step 3: `fraudRiskScore` is a number (0.0-1.0)
✅ Step 3: `claimType` is "auto"
✅ Step 3: `finalDecision` is made (ManualReview, AutoApprove, or Reject)
✅ Step 3: `processingTimeMs` is logged

**If all 7 checkmarks are visible: NLP is working! 🎉**

---

**Ready to test? Start with Step 1 in Swagger UI!**
