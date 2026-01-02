# 📋 Updated Request Payloads - File Path Based

**New Payload Format: Use FilePath Instead of Base64Content**

---

## Overview

**Change**: The API now accepts **file paths** instead of base64-encoded content.

**Benefits**:
✅ Simpler payload (no base64 encoding overhead)  
✅ Smaller network payload size  
✅ Better performance  
✅ Server-side file reading (more secure)  
✅ Easier to test and use  

---

## Step 1: Submit a Claim (Simple)

**Endpoint**: `POST /api/claims`

### Request Payload

```json
{
  "policyId": "POL-2024-567890",
  "claimantId": "CLMT-JOHN-DAVIS",
  "totalAmount": 8850.00
}
```

### Expected Response (200 OK)

```json
{
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Submitted",
  "submittedDate": "2024-01-20T10:00:00Z",
  "message": "Claim submitted successfully"
}
```

---

## Step 2: Add Document to Claim

**Endpoint**: `POST /api/claims/{claimId}/documents`

### Request Payload

```json
{
  "documentType": "AccidentReport",
  "filePath": "c:\\Hackathon Projects\\TestDocuments\\sample-claim-document.txt"
}
```

**Field Descriptions**:
- `documentType`: Type of document (AccidentReport, MedicalReport, etc.)
- `filePath`: **Full path to document file on server**
  - Use `\\` (double backslash) for Windows paths
  - File must exist and be readable
  - Example: `c:\\Hackathon Projects\\TestDocuments\\sample-claim-document.txt`

### What Happens:
1. Server reads file from `filePath`
2. File is converted to base64 internally
3. Document is stored in database
4. Base64 content is used for processing

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

---

## Step 3: Process Claim

**Endpoint**: `POST /api/claims/{claimId}/process`

### Request Payload

**No body needed** - Leave empty

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
      "extractedText": "CLAIM INFORMATION...",
      "confidence": 0.95,
      "classifiedType": "AccidentReport"
    }
  ],
  
  "nlpAnalysis": {
    "summary": "Auto accident claim for John Davis...",
    "fraudRiskScore": 0.28,
    "detectedEntities": "{...}",
    "claimType": "auto"
  },
  
  "mlScoring": {
    "fraudScore": 0.38,
    "approvalScore": 0.65,
    "fraudRiskLevel": "Low"
  },
  
  "processingTimeMs": 2847.5
}
```

---

## NEW! Combined Endpoint: Submit, Add Document, and Process

**Endpoint**: `POST /api/claims/submit-and-process`

**This is the fastest way to test everything in one call!**

### Request Payload

```json
{
  "policyId": "POL-2024-567890",
  "claimantId": "CLMT-JOHN-DAVIS",
  "totalAmount": 8850.00,
  "documents": [
    {
      "documentType": "AccidentReport",
      "filePath": "c:\\Hackathon Projects\\TestDocuments\\sample-claim-document.txt"
    }
  ]
}
```

**Field Descriptions**:
- `policyId`: Insurance policy number
- `claimantId`: Claimant identifier
- `totalAmount`: Total claim amount
- `documents`: Array of documents
  - `documentType`: Type of document
  - `filePath`: **Full path to document file** (not base64!)

**What Happens Internally**:
1. Server reads file from `filePath`
2. File is converted to base64 (internal step, automatic)
3. Claim is created
4. Document is attached
5. Complete NLP pipeline is executed
6. Results are returned

### Expected Response (200 OK)

```json
{
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "success": true,
  "finalDecision": "ManualReview",
  "decisionReason": "Requires manual review: Moderate risk (Fraud: 0.38, Approval: 0.65)",
  
  "ocrResults": [...],
  
  "rulesValidation": {
    "isValid": true,
    "reason": "All business rules passed"
  },
  
  "nlpAnalysis": {
    "summary": "Auto accident claim for John Davis. Vehicle collision at Main and Oak intersection on Jan 15, 2024. Total damages $8,850 after deductible. Two witnesses present.",
    "fraudRiskScore": 0.28,
    "detectedEntities": "{\"names\": [\"John Michael Davis\", \"Robert James Thompson\"], \"dates\": [\"January 15, 2024\"], \"locations\": [\"Springfield, IL\"], \"amounts\": [8850.00]}",
    "claimType": "auto"
  },
  
  "mlScoring": {
    "fraudScore": 0.38,
    "approvalScore": 0.65,
    "fraudRiskLevel": "Low"
  },
  
  "processingTimeMs": 2847.5
}
```

---

## Error Handling

### File Not Found

**Request**:
```json
{
  "policyId": "POL-2024-567890",
  "claimantId": "CLMT-JOHN-DAVIS",
  "totalAmount": 8850.00,
  "documents": [
    {
      "documentType": "AccidentReport",
      "filePath": "c:\\nonexistent\\file.txt"
    }
  ]
}
```

**Response (400 Bad Request)**:
```json
{
  "error": "File not found at path: c:\\nonexistent\\file.txt"
}
```

### Missing File Path

**Request**:
```json
{
  "policyId": "POL-2024-567890",
  "claimantId": "CLMT-JOHN-DAVIS",
  "totalAmount": 8850.00,
  "documents": [
    {
      "documentType": "AccidentReport",
      "filePath": ""
    }
  ]
}
```

**Response (400 Bad Request)**:
```json
{
  "error": "File path is required for document"
}
```

---

## Comparison: Old vs New

| Aspect | Old (Base64) | New (FilePath) |
|--------|--------------|----------------|
| **Payload** | Large (base64 encoded) | Small (just path string) |
| **Client Work** | Encode file to base64 | Simple: just provide path |
| **Server Work** | Decode base64 | Read file, convert to base64 |
| **Network Size** | ~33% larger | Minimal |
| **Performance** | Slower (large payload) | Faster (small payload) |
| **Example** | `"base64Content": "JVBERi0x..."` | `"filePath": "c:\\...\\file.txt"` |

---

## Quick Reference

### Fastest Testing (One Call)

```powershell
# Copy-paste into Swagger UI:
# Endpoint: POST /api/claims/submit-and-process

{
  "policyId": "POL-2024-567890",
  "claimantId": "CLMT-JOHN-DAVIS",
  "totalAmount": 8850.00,
  "documents": [
    {
      "documentType": "AccidentReport",
      "filePath": "c:\\Hackathon Projects\\TestDocuments\\sample-claim-document.txt"
    }
  ]
}
```

---

## File Path Rules

✅ **DO**:
- Use absolute paths: `c:\Hackathon Projects\TestDocuments\file.txt`
- Use double backslashes: `c:\\Hackathon Projects\\...`
- Ensure file exists and is readable
- Use valid document types

❌ **DON'T**:
- Use single backslashes: `c:\Hackathon Projects\...` (wrong!)
- Use relative paths without checking
- Use non-existent files
- Include special characters without escaping

---

## Example File Paths

**Windows**:
```
c:\\Hackathon Projects\\TestDocuments\\sample-claim-document.txt
c:\\Users\\YourName\\Documents\\claim.pdf
d:\\Claims\\2024\\claim-001.pdf
```

**UNC Paths** (network shares):
```
\\\\server\\share\\claims\\claim.pdf
```

---

## Implementation Changes

### What Changed in Code

1. **DTO Update**: `DocumentUploadDto` now has:
   - ✅ `FilePath` property (new, required)
   - ✅ `Base64Content` property (internal use only)
   - ❌ `FileName` property (removed)

2. **Controller Update**: `SubmitAndProcess` endpoint now:
   - Reads file from `FilePath` on disk
   - Converts to base64 automatically
   - Returns error if file not found
   - Logs file loading operations

3. **Service Layer**: Unchanged (continues to work with base64 internally)

---

## Testing Checklist

- [ ] Verify `sample-claim-document.txt` exists
- [ ] Verify file path is correct (Windows double backslashes)
- [ ] Submit claim with document using file path
- [ ] Verify file is read successfully (check logs)
- [ ] Verify NLP analysis completes
- [ ] Verify response includes nlpAnalysis object
- [ ] Verify fraud scores are calculated
- [ ] Verify no base64 errors in logs

---

## Success Indicators

✅ File read successfully from path  
✅ Document attached to claim  
✅ NLP analysis completed  
✅ Response includes summary, fraud score, entities  
✅ Processing time < 5 seconds  
✅ No file-not-found errors  

