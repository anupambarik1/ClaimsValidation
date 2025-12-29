# POC Test Instructions

## Prerequisites Setup

### 1. Gmail SMTP Configuration (Optional - for email testing)
To test email notifications:

1. Create or use existing Gmail account
2. Enable 2-Factor Authentication
3. Generate App Password:
   - Go to: https://myaccount.google.com/apppasswords
   - Select "Mail" and "Windows Computer"
   - Copy the 16-character password

4. Update `src/Claims.Api/appsettings.json`:
   ```json
   "SmtpSettings": {
     "Username": "your-email@gmail.com",
     "Password": "your-app-password-here"
   }
   ```

**Note**: If SMTP is not configured, emails will be logged to console only.

---

## Running the POC

### 1. Start the API
```powershell
cd "c:\Hackathon Projects\src\Claims.Api"
dotnet run
```

Expected output:
- "Training fraud detection model..." (first run only)
- "Model Metrics: Accuracy: XX%"
- "Model saved to: ../../MLModels/fraud-model.zip"
- "Using existing fraud detection model..." (subsequent runs)
- API listening on https://localhost:5001

### 2. Open Swagger UI
Navigate to: **https://localhost:5001/swagger**

---

## Testing End-to-End Flow

### Test 1: Low-Amount Claim (Auto-Approve)

**POST** `/api/claims`

Request Body:
```json
{
  "policyId": "POL-TEST-001",
  "claimantId": "test.user@example.com",
  "totalAmount": 500.00,
  "documents": [
    {
      "documentType": "Invoice",
      "fileName": "invoice.pdf",
      "base64Content": ""
    }
  ]
}
```

**Expected Result**:
- Status: 200 OK
- Response includes `claimId`
- Fraud Score: ~0.1 - 0.3
- Approval Score: ~0.7 - 0.9
- Decision: "AutoApprove" (if fraud < 0.3, approval > 0.8)
- Console shows: "Email sent to test.user@example.com"

---

### Test 2: High-Amount Claim (Manual Review)

Request Body:
```json
{
  "policyId": "POL-TEST-002",
  "claimantId": "test.user@example.com",
  "totalAmount": 5500.00,
  "documents": [
    {
      "documentType": "Invoice",
      "fileName": "invoice.pdf",
      "base64Content": ""
    }
  ]
}
```

**Expected Result**:
- Fraud Score: ~0.4 - 0.7
- Approval Score: ~0.3 - 0.6
- Decision: "ManualReview"

---

### Test 3: Very High-Amount Claim (Likely Reject)

Request Body:
```json
{
  "policyId": "POL-TEST-003",
  "claimantId": "test.user@example.com",
  "totalAmount": 10000.00,
  "documents": [
    {
      "documentType": "Invoice",
      "fileName": "invoice.pdf",
      "base64Content": ""
    },
    {
      "documentType": "Receipt",
      "fileName": "receipt.pdf",
      "base64Content": ""
    }
  ]
}
```

**Expected Result**:
- Fraud Score: ~0.7+ (high risk)
- Decision: "Reject" (if fraud > 0.7)

---

### Test 4: Get Claim Status

After submitting a claim, copy the `claimId` from response.

**GET** `/api/claims/{claimId}/status`

Replace `{claimId}` with actual GUID.

**Expected Result**:
```json
{
  "claimId": "guid-here",
  "status": "Processing",
  "submittedDate": "2025-12-11T...",
  "lastUpdatedDate": "2025-12-11T...",
  "fraudScore": 0.15,
  "approvalScore": 0.85
}
```

---

### Test 5: Get All User Claims

**GET** `/api/claims/user/test.user@example.com`

**Expected Result**: Array of all claims for that user.

---

## Verifying AI Components

### 1. ML.NET Fraud Detection ✅
- **Location**: `MLModels/fraud-model.zip`
- **Training Data**: `MLModels/claims-training-data.csv`
- **Verification**: 
  - First run shows "Training fraud detection model..."
  - Model metrics displayed (Accuracy, AUC, F1 Score)
  - Different claim amounts produce different fraud scores

### 2. Tesseract OCR ✅
- **Location**: `tessdata/eng.traineddata`
- **Verification**:
  - OCR will be called if document has actual file path
  - For POC, documents are stored in-memory
  - To test OCR: Place image in `TestDocuments/` and update `blobUri` in code

### 3. MailKit Email ✅
- **Verification**:
  - If SMTP configured: Real emails sent
  - If not configured: Console logs show "[Email Not Sent - SMTP Not Configured]"
  - Check inbox for "Claim Received" email

---

## Expected Console Output

```
Training fraud detection model...
Model Metrics:
  Accuracy: 85.00%
  AUC: 90.00%
  F1 Score: 82.00%
Model saved to: ../../MLModels/fraud-model.zip

info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000

[Email Not Sent - SMTP Not Configured] To: test.user@example.com, Subject: Claim Received
```

---

## Testing with Actual OCR

To test Tesseract OCR with real documents:

1. Place test images in `TestDocuments/` folder
2. Update `OcrService` or create test endpoint
3. Images should contain typed text (invoices, receipts)
4. OCR confidence typically 85-95% for clear typed documents

---

## Troubleshooting

### Issue: Model training fails
- **Check**: `MLModels/claims-training-data.csv` exists
- **Fix**: File should be created automatically

### Issue: Tesseract error "eng.traineddata not found"
- **Check**: `tessdata/eng.traineddata` exists (~50MB file)
- **Fix**: Re-download from GitHub

### Issue: Email not sending
- **Expected**: SMTP not configured = console logging only
- **To Enable**: Add Gmail credentials to appsettings.json

### Issue: All fraud scores are same
- **Check**: Model training succeeded
- **Fix**: Delete `MLModels/fraud-model.zip` and restart API

---

## Success Criteria

✅ API starts without errors  
✅ Swagger UI accessible  
✅ Model training completes on first run  
✅ Submit claim returns 200 OK  
✅ Different amounts produce different fraud scores  
✅ Email logged to console (or sent if SMTP configured)  
✅ Get status returns claim details  

---

## Production Notes

For production deployment:
- Replace in-memory database with SQL Server
- Add blob storage for actual document files
- Configure real SMTP service or Azure Communication Services
- Add authentication (Azure AD B2C)
- Implement historical claim lookup for better ML features
- Add more training data for improved model accuracy
- Implement actual OCR document upload workflow

---

**POC Version**: 1.0  
**Last Updated**: December 11, 2025
