# Testing Guide: Using Sample Documents with Claims API

## Sample Document Available

**Location**: `TestDocuments/sample-claim-document.txt`

This file contains a complete sample auto insurance claim with:
- Claim and policyholder information
- Detailed accident description
- Injury and damage information
- Total claim amount: $8,850.00 (after deductible)
- Supporting documentation lists
- Witness information
- Complete claimant declaration

---

## How to Test the API with This Document

### Option 1: Using AWS S3 (If S3 is configured)

1. Upload the document to your S3 bucket:
   ```
   s3://claims-documents-validation/sample-claim-document.txt
   ```

2. Call the API with S3 URI:
   ```json
   POST /api/claims/{claimId}/documents
   {
     "filePath": "s3://claims-documents-validation/sample-claim-document.txt",
     "documentType": "AccidentReport"
   }
   ```

### Option 2: Using Local File Path

1. Copy the sample document to your local machine:
   ```
   c:\Hackathon Projects\TestDocuments\sample-claim-document.txt
   ```

2. Call the API with local path:
   ```json
   POST /api/claims/{claimId}/documents
   {
     "filePath": "c:\\Hackathon Projects\\TestDocuments\\sample-claim-document.txt",
     "documentType": "AccidentReport"
   }
   ```

### Option 3: Complete End-to-End Test Flow

#### Step 1: Submit a Claim
```bash
POST http://localhost:5000/api/claims
Content-Type: application/json

{
  "policyId": "POL-2024-567890",
  "claimantId": "CLMT-JOHN-DAVIS",
  "totalAmount": 8850.00
}
```

**Response**:
```json
{
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Submitted",
  "submittedDate": "2024-01-20T10:00:00Z",
  "message": "Claim submitted successfully"
}
```

Save the `claimId` for next steps.

#### Step 2: Add the Sample Document
```bash
POST http://localhost:5000/api/claims/550e8400-e29b-41d4-a716-446655440000/documents
Content-Type: application/json

{
  "filePath": "c:\\Hackathon Projects\\TestDocuments\\sample-claim-document.txt",
  "documentType": "AccidentReport"
}
```

**Response**:
```json
{
  "documentId": "660f8500-f39c-51e5-b817-557766551111",
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "documentType": "AccidentReport",
  "uploadedDate": "2024-01-20T10:05:00Z",
  "message": "Document added successfully"
}
```

#### Step 3: Process the Claim (Triggers NLP)
```bash
POST http://localhost:5000/api/claims/550e8400-e29b-41d4-a716-446655440000/process
```

**Response** (includes NLP results):
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
      "extractedText": "CLAIM INFORMATION Claim Number: CLM-2024-001234...",
      "confidence": 0.95,
      "classifiedType": "AccidentReport"
    }
  ],
  
  "nlpAnalysis": {
    "summary": "Auto accident claim for John Davis. Vehicle collision at Main and Oak intersection on Jan 15, 2024. Total damages $8,850 after deductible. Two witnesses present.",
    "fraudRiskScore": 0.28,
    "detectedEntities": "{\"names\": [\"John Michael Davis\", \"Robert James Thompson\", \"Sarah Michelle Johnson\", \"Robert Chen\"], \"dates\": [\"January 15, 2024\", \"January 20, 2024\"], \"locations\": [\"Springfield, IL\"], \"amounts\": [8850.00, 8500.00, 450.00], \"claimType\": \"auto\"}",
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

## What This Tests

The sample document tests:

✅ **OCR Processing**
- Text extraction from document
- Confidence score calculation
- Document classification

✅ **NLP Analysis** (Our new feature!)
- **Summarization**: Bedrock creates a 2-3 sentence summary of the claim
- **Fraud Analysis**: Bedrock analyzes fraud risk, Comprehend analyzes sentiment
- **Entity Extraction**: Comprehend + Bedrock extract names, dates, amounts, locations
- **Claim Classification**: Bedrock classifies as "auto" claim type

✅ **Fraud Score Combination**
- ML fraud score: Generated from traditional features
- NLP fraud score: Generated from Bedrock analysis
- Combined score: 60% ML + 40% NLP

✅ **Decision Making**
- Auto-approve (low risk)
- Reject (high risk)
- Manual review (moderate risk)

---

## Expected NLP Results

**For this specific sample document**, you should expect:

| Metric | Expected Value | Reason |
|--------|----------------|--------|
| Fraud Risk Score (NLP) | 0.2 - 0.4 | Clear, detailed narrative; witnesses present; police report filed |
| Claim Type | "auto" | Clearly states vehicle accident |
| Entities Found | 4+ names, 2+ dates, 3+ amounts | Rich information in document |
| Summary | 2-3 sentences | Summarizes key points of accident |
| Final Decision | "ManualReview" | Legitimate claim, but manual review for verification |

---

## Converting to PDF (Optional)

If you need a real PDF file:

**Option 1: Windows Print to PDF**
1. Open `sample-claim-document.txt` in Notepad
2. Press `Ctrl+P` (Print)
3. Select "Microsoft Print to PDF"
4. Save as `sample-claim-document.pdf`

**Option 2: Online Converter**
1. Go to https://html2pdf.com
2. Copy content and paste
3. Generate PDF

**Option 3: Using Chrome/Edge**
```powershell
chrome --headless --disable-gpu --print-to-pdf="sample-claim-document.pdf" "sample-claim-document.txt"
```

---

## Additional Test Cases

You can create variations of this document to test different scenarios:

### Low Risk Claim
- Clear, detailed narrative ✅
- All information provided ✅
- Police report filed ✅
- **Expected**: Auto-approve

### High Risk Claim
- Vague descriptions
- Missing information
- Inconsistencies
- **Expected**: Reject or investigate

### Medium Risk Claim
- Borderline information
- Some missing details
- **Expected**: Manual review

---

## Testing Checklist

- [ ] Sample document created
- [ ] Claim submitted successfully
- [ ] Document added to claim
- [ ] Claim processed
- [ ] NLP analysis returned
- [ ] Fraud score calculated (combined)
- [ ] Decision made
- [ ] Check logs for NLP steps

---

**You're ready to test the NLP integration!**

Use this sample document to validate that:
1. Documents are processed correctly
2. NLP analysis generates meaningful insights
3. Fraud scores are reasonable (0.0-1.0 range)
4. Final decisions align with fraud risk
