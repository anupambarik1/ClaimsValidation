# API Test Payloads & Sample Data

Complete collection of sample request payloads for testing the Claims Validation API via Swagger UI.

---

## Quick Start

1. Run the application: `cd src\Claims.Api; dotnet run`
2. Open Swagger UI: http://localhost:5159/swagger
3. Copy-paste the JSON payloads below into the request body fields

---

## Test Scenarios

### ✅ Scenario 1: Low Risk Claim - Auto Approved
**Expected Result:** Status = `Approved`, FraudScore ~15-35%

**POST** `/api/claims`

```json
{
  "policyId": "POL-2024-001",
  "claimantId": "CLM-001",
  "totalAmount": 2500.00,
  "documents": [
    {
      "documentType": "Invoice",
      "fileName": "repair-invoice.pdf",
      "base64Content": "JVBERi0xLjQKJeLjz9MKMSAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCg=="
    },
    {
      "documentType": "Receipt",
      "fileName": "payment-receipt.pdf",
      "base64Content": "JVBERi0xLjQKJeLjz9MKMSAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCg=="
    }
  ]
}
```

**Why Low Risk:**
- Amount under $5,000
- Few documents (2)
- Valid policy and claimant IDs

---

### ❌ Scenario 2: High Risk Fraud - Auto Rejected
**Expected Result:** Status = `Rejected`, FraudScore ~75-95%, ValidationErrors included

**POST** `/api/claims`

```json
{
  "policyId": "POL-2024-002",
  "claimantId": "CLM-002",
  "totalAmount": 15000.00,
  "documents": [
    {
      "documentType": "Invoice",
      "fileName": "invoice1.pdf",
      "base64Content": "JVBERi0xLjQKJeLjz9MKMSAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCg=="
    },
    {
      "documentType": "Receipt",
      "fileName": "receipt1.pdf",
      "base64Content": "JVBERi0xLjQKJeLjz9MKMSAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCg=="
   ultiple documents (4)
- Large claim amount

---

### ⚠️ Scenario 3: Medium Risk - Under Review
**Expected Result:** Status = `Pending` or `UnderReview`, FraudScore ~40-60%

**POST** `/api/claims`

```json
{
  "policyId": "POL-2024-003",
  "claimantId": "CLM-003",
  "totalAmount": 5500.00,
  "documents": [
    {
      "documentType": "Invoice",
      "fileName": "water-damage-invoice.pdf",
      "base64Content": "JVBERi0xLjQKJeLjz9MKMSAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCg=="
    },
    {
      "documentType": "Receipt",
      "fileName": "repair-receipt.pdf",
      "base64Content": "JVBERi0xLjQKJeLjz9MKMSAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCg=="
    },
    {
      "documentType": "Other",
      "fileName": "photos.pdf",
      "base64Content": "JVBERi0xLjQKJeLjz9MKMSAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCg=="
    }
  ]ys ago)

---

### ⚠️ Scenario 3: Medium Risk - Under Review
**Expected Result:** Status = `Pending` or `UnderReview`, FraudScore ~40-60%

**POST** `/api/claims`

```json
{
  "claimantName": "Robert Johnson",
  Medium claim amount

---

### ✅ Scenario 4: First Time Claimant - Low Risk
**Expected Result:** Status = `Approved`, FraudScore ~10-25%

**POST** `/api/claims`

```json
{
  "policyId": "POL-2024-004",
  "claimantId": "CLM-004",
  "totalAmount": 1200.00,
  "documents": [
    {
      "documentType": "Invoice",
      "fileName": "windshield-invoice.pdf",
      "base64Content": "JVBERi0xLjQKJeLjz9MKMSAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCg=="
    }
  ] = `Approved`, FraudScore ~10-25%

**POST** `/api/claims`

```json
{Low-risk claim type

---

### 🔥 Scenario 5: Extreme Fraud Pattern
**Expected Result:** Status = `Rejected`, FraudScore ~90-99%

**POST** `/api/claims`

```json
{
  "policyId": "POL-2024-005",
  "claimantId": "CLM-005",
  "totalAmount": 25000.00,
  "documents": [
    {
      "documentType": "Invoice",
      "fileName": "invoice.pdf",
      "base64Content": "JVBERi0xLjQKJeLjz9MKMSAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCg=="
    },
    {
      "documentType": "Receipt",
      "fileName": "receipt.pdf",
      "base64Content": "JVBERi0xLjQKJeLjz9MKMSAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCg=="
    },
    {
      "documentType": "MedicalReport",
      "fileName": "medical.pdf",
      "base64Content": "JVBERi0xLjQKJeLjz9MKMSAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCg=="
    },
    {
      "documentType": "PolicyDocument",
      "fileName": "policy.pdf",
      "base64Content": "JVBERi0xLjQKJeLjz9MKMSAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCg=="
    },
    {
  Multiple documents (5)
- Excessive claim amount
---

### 🚗 Scenario 8: Multiple Small Claims Pattern
**Expected Result:** Status = `UnderReview`, FraudScore ~55-70%

**POST** `/api/claims`

```json
{
  "claimantName": "Lisa Thompson",
  "claimantEmail": "lisa.t@example.com",
  "amount": 3500.00,
  "description": "Tire damage and alignment",
  "documentCount": 3,
  "claimantHistoryCount": 7,
  "daysSinceLastClaim": 25
}
```

**Why Suspicious:**
- Moderate amount but frequent claims
- High claim history (7 claims)
- Recent last claim (25 days)
- Pattern of frequent small claims

---

## GET Requests

### Get Claim by ID
**GET** `/api/claims/{id}`

1. First submit a claim using any POST scenario above
2. Copy the returned `id` from the response (GUID format)
3. Paste it into the `id` parameter field in Swagger

**Example ID:** `3fa85f64-5717-4562-b3fc-2c963f66afa6`

---

### Get All Claims
**GET** `/api/claims`

No request body needed - just click "Execute" to see all submitted claims.

---

## Understanding the Response

### Sample Response Structure
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "claimantName": "John Smith",
  "claimantEmail": "john.smith@example.com",
  "amount": 2500.00,
  "description": "Minor vehicle damage...",
  "status": "Approved",
  "fraudScore": 0.23,
  "submittedDate": "2025-12-19T10:30:00Z",
  "validationErrors": [],
  "ocrResults": {
    "extractedText": "Sample OCR text...",
    "confidence": 0.92
  },
  "mlScore": {
    "fraudProbability": 0.23,
    "confidence": 0.88
  }
}
```

### Status Values
- **Approved**: Low fraud risk, automatically approved
- **Pending**: Awaiting additional review
- **UnderReview**: Moderate risk, requires manual review
- **Rejected**: High fraud risk, automatically rejected

### Fraud Score Interpretation
---

## Additional Information

### Request Model Structure

The API expects the following structure:

```jsonclaimId` from the response (GUID format)
3. Paste it into the `claimId` parameter field in Swagger

**Example claimId:** `3fa85f64-5717-4562-b3fc-2c963f66afa6`
  ]
}
```

### Document Types
- **Invoice**: Repair or service invoices
- **Receipt**: Payment receipts
- **MedicalReport**: Medical documentation
- **PolicyDocument**: Insurance policy documents
- **IdentityProof**: ID verification documents
- **Other**: Any other supporting documents

### Base64 Content

**POST Response (ClaimResponseDto):**
```json
{
  "claimId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Approved",
  "message": "Claim submitted successfully",
  "fraudScore": 0.23,
  "validationErrors": [],
  "submittedAt": "2025-12-19T10:30:00Z"
}
```

**GET Status Response (ClaimStatusDto):**
```json
{
  "claimId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "policyId": "POL-2024-001",
  "claimantId": "CLM-001",
  "status": "Approved",
  "totalAmount": 2500.00,
  "fraudScore": 0.23,
  "submittedDate": "2025-12-19T10:30:00Z",
  "lastUpdatedDate": "2025-12-19T10:30:05Z",
  "ocrProcessed": true,
  "mlProcessed": true,
  "notificationSent": true
---

## Testing Tips

### 1. Progressive Testing
Test scenarios in order (1→8) to see the full spectrum of fraud detection

### 2. Modify Values
Experiment by changing individual fields:
- **Amount**: Try 500, 5000, 15000, 25000
- **DocumentCount**: Try 1, 3, 5, 10
- **ClaimantHistoryCount**: Try 0, 2, 5, 10
- **DaysSinceLastClaim**: Try 7, 30, 90, 365

### 3. Extreme Values
Test boundary conditions:
- MiTotalAmount**: Try 500, 5000, 15000, 25000
- **PolicyId**: Try different policy formats
- **ClaimantId**: Try different claimant IDs
- **Documents array**: Add/remove documents

### 3. Invalid Data
Test validation errors:
```json
{
  "policyId": "",
  "claimantId": "",
  "totalAmount": -100,
  "documents": []
}
```

Expected error: 400 Bad Request with validation messages
## Quick Reference Table

| Scenario | Amount | Docs | History | Days | Expected Status | Fraud Score |
|----------|--------|------|---------|------|-----------------|-------------|
| #1 Low Risk | $2,500 | 2 | 1 | 180 | Approved | 15-35% |
| #2 High Fraud | $15,000 | 8 | 6 | 15 | Rejected | 75-95% |
| #3 Medium | $5,500 | 3 | 2 | 90 | Pending | 40-60% |
| #44First Timer | $1,200 | 1 | 0 | 365 | Approved | 10-25% |
| #5 Extreme | $25,000 | 12 | 10 | 5 | Rejected | 90-99% |
| #6 Legit Large | $12,000 | 5 | 1 | 730 | Pending | 30-50% |
| #7 Borderline | $7,500 | 4 | 3 | 60 | UnderReview | 48-52% |
| #8 Frequent | $3,500 | 3 | 7 | 25 | UnderReview | 55-70% |

---

## PowerShellPolicy ID | Amount | Docs | Expected Status |
|----------|-----------|--------|------|-----------------|
| #1 Low Risk | POL-2024-001 | $2,500 | 2 | Approved |
| #2 High Fraud | POL-2024-002 | $15,000 | 4 | Rejected |
| #3 Medium | POL-2024-003 | $5,500 | 3 | Pending |
| #4 First Timer | POL-2024-004 | $1,200 | 1 | Approved |
| #5 Extreme | POL-2024-005 | $25,000 | 5 | Rejected
    description = "Minor vehicle damage"
    documentCount = 2
    claimantHistoryCount = 1
    daysSinceLastClaim = 180
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5159/api/claims" -Method Post -Body $claim -ContentType "application/json"

# Get all claims
Invoke-RestMethod -Uri "http://localhost:5159/api/claims" -Method Get

# Get claim by ID (replace with actual ID)
Invoke-RestMethod -Uri "http://localhost:5159/api/claims/3fa85f64-5717-4562-b3fc-2c963f66afa6" -Method Get
```

---

## Troubleshooting

### API Not Running
```powershell
cd "c:\Hackathon Projects\src\Claims.Api"
dotnet run
```

### Model Training Failed
Check that `MLModels/claims-training-data.csv` exists with 30+ rows

### SMTP Errors
Email notifications fall back to console logging if SMTP not configured

### All Claims Showing Same Score
Restart the API - the ML model trains on first startup

---
policyId = "POL-2024-001"
    claimantId = "CLM-001"
    totalAmount = 2500.00
    documents = @(
        @{
            documentType = "Invoice"
            fileName = "invoice.pdf"
            base64Content = "JVBERi0xLjQKJeLjz9MKMSAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCg=="
        }
    )
} | ConvertTo-Json -Depth 10

$response = Invoke-RestMethod -Uri "http://localhost:5159/api/claims" -Method Post -Body $claim -ContentType "application/json"
Write-Output $response

# Get claim status (replace with actual claimId from response)
Invoke-RestMethod -Uri "http://localhost:5159/api/claims/$($response.claimId)/status