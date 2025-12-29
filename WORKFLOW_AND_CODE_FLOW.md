# Claims Validation System - Detailed Workflow & Code Flow Guide

## Table of Contents
1. [System Overview](#system-overview)
2. [Architecture Diagram](#architecture-diagram)
3. [Complete Code Flow - Step by Step](#complete-code-flow---step-by-step)
4. [API Request-Response Flow](#api-request-response-flow)
5. [Service Layer Interactions](#service-layer-interactions)
6. [Database Schema & Data Model](#database-schema--data-model)
7. [Processing Pipeline](#processing-pipeline)
8. [Decision Logic Flow](#decision-logic-flow)
9. [Data Structures Reference](#data-structures-reference)
10. [Execution Sequence Diagram](#execution-sequence-diagram)

---

## System Overview

The Claims Validation System is a **layered ASP.NET Core 9.0 application** that automates insurance claim processing using:
- **OCR (Tesseract)** - Extracts text from claim documents
- **ML.NET** - Detects fraud and predicts approval likelihood
- **Rules Engine** - Validates claims against business policies
- **Notification Service** - Sends status updates via email
- **Entity Framework Core** - Manages data persistence

### Technology Stack
```
┌────────────────────────────────────────────────────────────┐
│  Presentation Layer: ASP.NET Core 9.0 REST API             │
├────────────────────────────────────────────────────────────┤
│  Application Services: Business Logic & Orchestration       │
├────────────────────────────────────────────────────────────┤
│  Domain Layer: Models, DTOs, Enums                          │
├────────────────────────────────────────────────────────────┤
│  Infrastructure: EF Core, Database, Configurations          │
└────────────────────────────────────────────────────────────┘
```

---

## Architecture Diagram

### High-Level System Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          PRESENTATION LAYER                              │
│                      Claims.Api (ASP.NET Core 9.0)                       │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │  ClaimsController                    StatusController             │ │
│  │  ├─ POST /api/claims                 ├─ GET /api/status/health   │ │
│  │  ├─ GET /api/claims/{id}/status      └─ Health Checks            │ │
│  │  ├─ GET /api/claims/user/{userId}                                │ │
│  │  ├─ PUT /api/claims/{id}/status                                  │ │
│  │  └─ POST /api/claims/{id}/process                                │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────┬┘
                                 │
                                 │ HTTP Requests
                                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      APPLICATION SERVICES LAYER                          │
│                        Claims.Services                                    │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │  ClaimsService (Orchestrator)                                     │  │
│  │  ├─ SubmitClaimAsync()        ► Initiates claim processing       │  │
│  │  ├─ ProcessClaimAsync()       ► Main AI pipeline orchestration   │  │
│  │  ├─ GetClaimStatusAsync()     ► Retrieves claim details          │  │
│  │  └─ UpdateClaimStatusAsync()  ► Manual intervention              │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                  │                                        │
│  ┌──────────────────┐  ┌──────────────────┐  ┌─────────────────────┐   │
│  │  OcrService      │  │ RulesEngine      │  │ MlScoringService    │   │
│  │  (Tesseract)     │  │ Service          │  │ (ML.NET)            │   │
│  │                  │  │                  │  │                     │   │
│  │ • Extract text   │  │ • Validate policy│  │ • Fraud detection   │   │
│  │ • Set confidence │  │ • Coverage check │  │ • Approval scoring  │   │
│  │ • Mark status    │  │ • Limit check    │  │ • Decision logic    │   │
│  └──────────────────┘  └──────────────────┘  └─────────────────────┘   │
│                                  │                                        │
│  ┌──────────────────┐  ┌──────────────────────────────────────────────┐ │
│  │  Notification    │  │  DocumentAnalysisService                     │ │
│  │  Service         │  │  • Classify document type                    │ │
│  │  (MailKit SMTP)  │  │  • Validate format                           │ │
│  │                  │  │  • Extract key information                   │ │
│  │ • Send emails    │  └──────────────────────────────────────────────┘ │
│  │ • Track status   │                                                    │
│  └──────────────────┘                                                    │
└──────────────────────────────────────────────────────────────────────────┘
                                 │
                                 │ EntityFramework Queries
                                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         DOMAIN LAYER                                     │
│                      Claims.Domain                                        │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │  Entities: Claim, Document, Decision, Notification               │  │
│  │  Enums: ClaimStatus, DecisionStatus, OcrStatus, etc.             │  │
│  │  DTOs: ClaimSubmissionDto, ClaimStatusDto, ClaimResponseDto      │  │
│  └───────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────┘
                                 │
                                 │ DbContext Commands
                                 ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      INFRASTRUCTURE LAYER                                │
│                    Claims.Infrastructure                                 │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │  ClaimsDbContext (EF Core)                                        │  │
│  │  ├─ DbSet<Claim>         ┌──────────────────────────────────┐    │  │
│  │  ├─ DbSet<Document>      │  SQL Server / In-Memory Database │    │  │
│  │  ├─ DbSet<Decision>      │  • Table: Claims                 │    │  │
│  │  └─ DbSet<Notification>  │  • Table: Documents             │    │  │
│  │                          │  • Table: Decisions             │    │  │
│  │  Entity Configurations   │  • Table: Notifications         │    │  │
│  │  └─ Fluent API setup     └──────────────────────────────────┘    │  │
│  └───────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Complete Code Flow - Step by Step

### Flow 1: Claim Submission Flow (POST /api/claims)

```
USER
  │
  ├─► HTTP POST: ClaimSubmissionDto
  │   {
  │     "policyId": "POL-2025-001",
  │     "claimantId": "CLAIM-001",
  │     "totalAmount": 5000.00,
  │     "documents": [...]
  │   }
  │
  ▼
ClaimsController.SubmitClaim(submission)
  │
  ├─► IClaimsService.SubmitClaimAsync(submission)
  │
  ▼
ClaimsService.SubmitClaimAsync(submission)
  │
  ├─► Step 1: Create Claim Entity
  │   {
  │     ClaimId = Guid.NewGuid(),
  │     PolicyId = "POL-2025-001",
  │     ClaimantId = "CLAIM-001",
  │     TotalAmount = 5000.00,
  │     Status = ClaimStatus.Submitted,
  │     SubmittedDate = DateTime.UtcNow,
  │     LastUpdatedDate = DateTime.UtcNow
  │   }
  │
  ├─► Step 2: Save to Database
  │   _context.Claims.Add(claim)
  │   await _context.SaveChangesAsync()
  │
  ├─► Step 3: Send Initial Notification
  │   await _notificationService
  │     .SendClaimStatusNotificationAsync(
  │       claimId, claimantId, NotificationType.ClaimReceived)
  │
  ├─► Step 4: Return Response
  │   {
  │     "claimId": "550e8400-e29b-41d4-a716-446655440000",
  │     "status": "Submitted",
  │     "submittedDate": "2025-12-29T10:30:00Z",
  │     "message": "Claim submitted successfully"
  │   }
  │
  ▼
USER (Receives ClaimResponseDto with ClaimId)
```

---

### Flow 2: Complete Claim Processing Flow (POST /api/claims/{claimId}/process)

This is the **main AI pipeline** that processes a submitted claim:

```
USER
  │
  ├─► HTTP POST: /api/claims/{claimId}/process
  │
  ▼
ClaimsController.ProcessClaim(claimId)
  │
  ├─► IClaimsService.ProcessClaimAsync(claimId)
  │
  ▼
ClaimsService.ProcessClaimAsync(claimId)
  │
  ├─ RETRIEVE CLAIM FROM DATABASE
  │ var claim = await _context.Claims
  │   .Include(c => c.Documents)
  │   .FirstOrDefaultAsync(c => c.ClaimId == claimId)
  │
  ├─► Update Status → "Processing"
  │   claim.Status = ClaimStatus.Processing
  │
  ▼ ═══════════════════════════════════════════════════════════════
      STEP 1: OCR & DOCUMENT EXTRACTION (for each document)
  ═════════════════════════════════════════════════════════════════
  │
  ├─► For each document in claim.Documents:
  │   │
  │   ├─► IOcrService.ProcessDocumentAsync(document.BlobUri)
  │   │   │
  │   │   ├─► OcrService.ProcessDocumentAsync(blobUri)
  │   │   │
  │   │   ├─► Step 1.1: Load Document
  │   │   │   Load PDF/Image from blob storage
  │   │   │
  │   │   ├─► Step 1.2: Run Tesseract OCR
  │   │   │   Extract text from document
  │   │   │
  │   │   ├─► Step 1.3: Calculate Confidence
  │   │   │   Measure OCR accuracy (0.0 - 1.0)
  │   │   │
  │   │   └─► Return (extractedText, confidence)
  │   │
  │   ├─► Store OCR Results
  │   │   document.ExtractedText = extractedText
  │   │   document.OcrConfidence = confidence
  │   │   document.OcrStatus = (confidence >= 0.7) 
  │   │                          ? OcrStatus.Completed 
  │   │                          : OcrStatus.Failed
  │   │
  │   └─► Log: "Document {id} processed: Confidence={conf}"
  │
  ├─► Save Document Changes
  │   await _context.SaveChangesAsync()
  │
  ▼ ═══════════════════════════════════════════════════════════════
      STEP 2: DOCUMENT CLASSIFICATION (for each document)
  ═════════════════════════════════════════════════════════════════
  │
  ├─► For each document with extracted text:
  │   │
  │   ├─► IDocumentAnalysisService.ClassifyDocumentAsync(text)
  │   │   │
  │   │   ├─► DocumentAnalysisService.ClassifyDocumentAsync(text)
  │   │   │
  │   │   ├─► Step 2.1: Analyze Text Content
  │   │   │   - Look for keywords in text
  │   │   │   - Identify document patterns
  │   │   │
  │   │   ├─► Step 2.2: Classify Type
  │   │   │   - Medical Report
  │   │   │   - Police Report
  │   │   │   - Invoice/Receipt
  │   │   │   - Photo/Evidence
  │   │   │
  │   │   └─► Return DocumentType
  │   │
  │   └─► Store in ocrResults List
  │       ocrResults.Add({
  │         DocumentId = document.DocumentId,
  │         ExtractedText = extractedText,
  │         Confidence = confidence,
  │         ClassifiedType = docType,
  │         Success = true
  │       })
  │
  ▼ ═════════════════════════════════════════════════════════════════
      STEP 3: BUSINESS RULES VALIDATION
  ═════════════════════════════════════════════════════════════════
  │
  ├─► IRulesEngineService.ValidatePolicyAsync(claim)
  │   │
  │   ├─► RulesEngineService.ValidatePolicyAsync(claim)
  │   │
  │   ├─► Rule 1: Check Policy Validity
  │   │   - Is policy active?
  │   │   - Has policy expired?
  │   │   - Is policy in good standing?
  │   │
  │   ├─► Rule 2: Check Coverage Limits
  │   │   - Is claim amount within limits?
  │   │   - Has annual limit been exceeded?
  │   │   - Are deductibles applied?
  │   │
  │   ├─► Rule 3: Validate Claim Type
  │   │   - Is claimed loss covered?
  │   │   - Are exclusions applicable?
  │   │   - Is claim type valid?
  │   │
  │   └─► Return (isValid, validationReason)
  │
  ├─► If Rules Failed:
  │   │
  │   ├─► claim.Status = ClaimStatus.Rejected
  │   ├─► Create Decision (Rejected)
  │   ├─► Send Notification
  │   └─► Return Result with Rejection Reason
  │
  ▼ ═════════════════════════════════════════════════════════════════
      STEP 4: ML-BASED FRAUD DETECTION & SCORING
  ═════════════════════════════════════════════════════════════════
  │
  ├─► IMlScoringService.ScoreClaimAsync(claim)
  │   │
  │   ├─► MlScoringService.ScoreClaimAsync(claim)
  │   │
  │   ├─► Step 4.1: Prepare Feature Vector
  │   │   Extract features from claim:
  │   │   - Claim amount
  │   │   - Time of day submitted
  │   │   - Day of week
  │   │   - Document count
  │   │   - OCR confidence
  │   │   - Previous claims count
  │   │   - Policy age
  │   │   - etc.
  │   │
  │   ├─► Step 4.2: Load ML.NET Model
  │   │   (FastTree trained model from claims-training-data.csv)
  │   │
  │   ├─► Step 4.3: Run Fraud Detection
  │   │   model.Predict(featureVector)
  │   │   → fraudScore (0.0 - 1.0)
  │   │     • 0.0-0.4 = Low Risk
  │   │     • 0.4-0.7 = Medium Risk
  │   │     • 0.7-1.0 = High Risk
  │   │
  │   ├─► Step 4.4: Calculate Approval Score
  │   │   Based on:
  │   │   - OCR confidence
  │   │   - Rules validation
  │   │   - Document completeness
  │   │   → approvalScore (0.0 - 1.0)
  │   │
  │   └─► Return (fraudScore, approvalScore)
  │
  ├─► Store Scores in Database
  │   claim.FraudScore = fraudScore
  │   claim.ApprovalScore = approvalScore
  │
  ▼ ═════════════════════════════════════════════════════════════════
      STEP 5: AUTOMATED DECISION LOGIC
  ═════════════════════════════════════════════════════════════════
  │
  ├─► IMlScoringService.DetermineDecisionAsync(fraudScore, approvalScore)
  │   │
  │   ├─► MlScoringService.DetermineDecisionAsync(...)
  │   │
  │   ├─ DECISION TREE:
  │   │
  │   ├─► IF fraudScore > 0.7 AND approvalScore < 0.5
  │   │   └─► Decision = "Reject"
  │   │       Reason: "High fraud risk detected"
  │   │       claim.Status = ClaimStatus.Rejected
  │   │
  │   ├─► ELSE IF fraudScore < 0.4 AND approvalScore > 0.75
  │   │   └─► Decision = "AutoApprove"
  │   │       Reason: "Low fraud risk, high approval score"
  │   │       claim.Status = ClaimStatus.Approved
  │   │
  │   └─► ELSE
  │       └─► Decision = "ManualReview"
  │           Reason: "Moderate risk, requires manual review"
  │           claim.Status = ClaimStatus.UnderReview
  │
  ├─► Create Decision Record
  │   var decision = new Decision {
  │     DecisionId = Guid.NewGuid(),
  │     ClaimId = claimId,
  │     Status = DecisionStatus.[Approved|Rejected|PendingReview],
  │     DecisionDate = DateTime.UtcNow,
  │     Reason = decision reason,
  │     ReviewerId = isAutoDecision ? "AI_SYSTEM" : null
  │   }
  │   _context.Decisions.Add(decision)
  │
  ├─► Save All Changes
  │   claim.LastUpdatedDate = DateTime.UtcNow
  │   await _context.SaveChangesAsync()
  │
  ▼ ═════════════════════════════════════════════════════════════════
      STEP 6: SEND NOTIFICATION & RETURN RESULT
  ═════════════════════════════════════════════════════════════════
  │
  ├─► Send Status Notification
  │   await _notificationService
  │     .SendClaimStatusNotificationAsync(
  │       claimId, claimantId, NotificationType.DecisionMade)
  │
  ├─► Build Result
  │   {
  │     "claimId": "{claimId}",
  │     "success": true,
  │     "finalDecision": "AutoApprove|Reject|ManualReview",
  │     "decisionReason": "...",
  │     "ocrResults": [...],
  │     "rulesValidation": {...},
  │     "mlScoring": {
  │       "fraudScore": 0.25,
  │       "approvalScore": 0.82,
  │       "fraudRiskLevel": "Low"
  │     },
  │     "processingTimeMs": 5432
  │   }
  │
  ▼
USER (Receives ClaimProcessingResult)
```

---

## API Request-Response Flow

### 1. Submit Claim Request

**Endpoint:** `POST /api/claims`

**Request Body:**
```json
{
  "policyId": "POL-2025-001",
  "claimantId": "CLAIMANT-001",
  "totalAmount": 5000.50,
  "documents": [
    {
      "documentType": "MedicalReport",
      "fileName": "hospital_bill.pdf",
      "base64Content": "JVBERi0xLjQKJeLjz9MNCjEgMCBv..."
    }
  ]
}
```

**Response Body (201 Created):**
```json
{
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Submitted",
  "submittedDate": "2025-12-29T10:30:00Z",
  "message": "Claim submitted successfully"
}
```

---

### 2. Process Claim Request

**Endpoint:** `POST /api/claims/{claimId}/process`

**Request Parameters:**
- `claimId` (GUID path parameter): The claim ID to process

**Response Body (200 OK):**
```json
{
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "success": true,
  "finalDecision": "AutoApprove",
  "decisionReason": "Auto-approved: Low fraud risk (0.25), high approval score (0.82)",
  "processingTimeMs": 5432.50,
  "ocrResults": [
    {
      "documentId": "660e8400-e29b-41d4-a716-446655440001",
      "extractedText": "Hospital Bill... [extracted text content]",
      "confidence": 0.95,
      "classifiedType": "MedicalReport",
      "success": true
    }
  ],
  "rulesValidation": {
    "isValid": true,
    "reason": "Policy is valid and covers this claim type",
    "rulesChecked": ["PolicyLimit", "PolicyValidity", "CoverageCheck"]
  },
  "mlScoring": {
    "fraudScore": 0.25,
    "approvalScore": 0.82,
    "fraudRiskLevel": "Low"
  }
}
```

---

### 3. Get Claim Status Request

**Endpoint:** `GET /api/claims/{claimId}/status`

**Request Parameters:**
- `claimId` (GUID path parameter): The claim ID

**Response Body (200 OK):**
```json
{
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Approved",
  "submittedDate": "2025-12-29T10:30:00Z",
  "lastUpdatedDate": "2025-12-29T10:35:00Z",
  "totalAmount": 5000.50,
  "fraudScore": 0.25,
  "approvalScore": 0.82,
  "documents": [
    {
      "documentId": "660e8400-e29b-41d4-a716-446655440001",
      "documentType": "MedicalReport",
      "ocrStatus": "Completed",
      "ocrConfidence": 0.95
    }
  ],
  "decisions": [
    {
      "decisionId": "770e8400-e29b-41d4-a716-446655440002",
      "status": "Approved",
      "decisionDate": "2025-12-29T10:35:00Z",
      "reason": "Auto-approved: Low fraud risk (0.25), high approval score (0.82)",
      "reviewerId": "AI_SYSTEM"
    }
  ]
}
```

---

## Service Layer Interactions

### ClaimsService (Orchestrator)
Primary responsibility: **Coordinate all services in the correct sequence**

```
ClaimsService
│
├─ SubmitClaimAsync(submission)
│  ├─ Create Claim entity
│  ├─ Save to database
│  ├─ Call INotificationService.SendClaimStatusNotificationAsync()
│  └─ Return ClaimResponseDto
│
├─ ProcessClaimAsync(claimId)
│  ├─ Retrieve Claim with Documents
│  ├─ For each Document:
│  │  ├─ Call IOcrService.ProcessDocumentAsync()
│  │  ├─ Call IDocumentAnalysisService.ClassifyDocumentAsync()
│  │  └─ Update Document with OCR results
│  │
│  ├─ Call IRulesEngineService.ValidatePolicyAsync()
│  │  └─ If failed, Reject claim and Return
│  │
│  ├─ Call IMlScoringService.ScoreClaimAsync()
│  ├─ Call IMlScoringService.DetermineDecisionAsync()
│  │
│  ├─ Create Decision record
│  ├─ Update Claim status
│  ├─ Call INotificationService.SendClaimStatusNotificationAsync()
│  └─ Return ClaimProcessingResult
│
├─ GetClaimStatusAsync(claimId)
│  ├─ Query database for Claim
│  ├─ Load related Documents, Decisions, Notifications
│  └─ Return ClaimStatusDto
│
└─ UpdateClaimStatusAsync(claimId, status, specialistId)
   ├─ Find Claim by ID
   ├─ Update status
   ├─ Create Decision record
   └─ Save and return success flag
```

---

### IOcrService (Document Text Extraction)
Primary responsibility: **Extract text from documents using Tesseract OCR**

```
OcrService
│
├─ ProcessDocumentAsync(blobUri)
│  ├─ Step 1: Load Document from Blob Storage
│  │  └─ Download PDF/Image file
│  │
│  ├─ Step 2: Convert to Image Format (if PDF)
│  │  └─ Extract images from PDF pages
│  │
│  ├─ Step 3: Run Tesseract OCR
│  │  ├─ Initialize Tesseract engine
│  │  ├─ Load trained data (eng.traineddata)
│  │  └─ Extract text from image
│  │
│  ├─ Step 4: Calculate Confidence Score
│  │  ├─ Analyze mean confidence of OCR output
│  │  ├─ Check for incomplete lines
│  │  └─ Range: 0.0 (low) to 1.0 (high)
│  │
│  └─ Return (extractedText: string, confidence: decimal)
```

---

### IDocumentAnalysisService (Document Classification)
Primary responsibility: **Classify document type based on content**

```
DocumentAnalysisService
│
├─ ClassifyDocumentAsync(extractedText)
│  ├─ Step 1: Tokenize and Normalize Text
│  │
│  ├─ Step 2: Search for Document-Specific Keywords
│  │  ├─ Medical Report: "diagnosis", "patient", "treatment", "hospital"
│  │  ├─ Police Report: "officer", "incident", "report", "badge"
│  │  ├─ Invoice/Receipt: "invoice", "total", "amount", "qty"
│  │  └─ Photo/Evidence: [image analysis]
│  │
│  ├─ Step 3: Calculate Confidence Scores
│  │  └─ Match confidence for each document type
│  │
│  └─ Return DocumentType (highest match)
```

---

### IRulesEngineService (Business Rules Validation)
Primary responsibility: **Validate claim against business policies**

```
RulesEngineService
│
├─ ValidatePolicyAsync(claim)
│  ├─ Rule 1: Policy Validity Check
│  │  ├─ Check if policy exists in system
│  │  ├─ Verify policy is not expired
│  │  └─ Confirm policy is active
│  │
│  ├─ Rule 2: Coverage Check
│  │  ├─ Verify claim type is covered
│  │  ├─ Check exclusions don't apply
│  │  └─ Validate waiting period (if applicable)
│  │
│  ├─ Rule 3: Amount Limits Check
│  │  ├─ Check claim amount ≤ policy limit
│  │  ├─ Check annual limit not exceeded
│  │  └─ Apply deductibles if applicable
│  │
│  └─ Return (isValid: bool, reason: string)
```

---

### IMlScoringService (Fraud Detection & Scoring)
Primary responsibility: **Run ML model for fraud detection and approval scoring**

```
MlScoringService
│
├─ ScoreClaimAsync(claim)
│  ├─ Step 1: Extract Features from Claim
│  │  ├─ Claim amount
│  │  ├─ Time of submission (hour, day of week)
│  │  ├─ Document count
│  │  ├─ OCR confidence scores
│  │  ├─ Policy age (days)
│  │  └─ Previous claims count
│  │
│  ├─ Step 2: Load ML.NET FastTree Model
│  │  └─ Trained on claims-training-data.csv
│  │
│  ├─ Step 3: Generate Fraud Prediction
│  │  ├─ Input: Feature vector
│  │  ├─ Process: ML.NET model prediction
│  │  └─ Output: fraudScore (0.0 - 1.0)
│  │
│  ├─ Step 4: Calculate Approval Score
│  │  ├─ Based on: OCR confidence + document quality + rules validation
│  │  └─ Output: approvalScore (0.0 - 1.0)
│  │
│  └─ Return (fraudScore, approvalScore)
│
├─ DetermineDecisionAsync(fraudScore, approvalScore)
│  ├─ IF fraudScore > 0.7 AND approvalScore < 0.5
│  │  └─ Return "Reject"
│  │
│  ├─ ELSE IF fraudScore < 0.4 AND approvalScore > 0.75
│  │  └─ Return "AutoApprove"
│  │
│  └─ ELSE
│     └─ Return "ManualReview"
```

---

### INotificationService (Email Notifications)
Primary responsibility: **Send email notifications using MailKit SMTP**

```
NotificationService
│
├─ SendClaimStatusNotificationAsync(claimId, claimantId, notificationType)
│  ├─ Step 1: Build Email Content
│  │  ├─ Load email template based on notification type
│  │  ├─ Insert claim details
│  │  └─ Add decision/status information
│  │
│  ├─ Step 2: Compose Email Message
│  │  ├─ To: claimant email address
│  │  ├─ Subject: "Claim {ClaimId} Status Update"
│  │  ├─ Body: HTML formatted message
│  │  └─ From: system email address
│  │
│  ├─ Step 3: Send via SMTP (MailKit)
│  │  ├─ Connect to SMTP server
│  │  ├─ Authenticate with credentials
│  │  └─ Send message
│  │
│  ├─ Step 4: Log Notification in Database
│  │  └─ Create Notification record
│  │
│  └─ Return success flag
```

---

## Database Schema & Data Model

### Entity Relationships

```
┌─────────────────────┐
│      Claims         │
├─────────────────────┤
│ ClaimId (PK)        │
│ PolicyId            │
│ ClaimantId          │
│ Status              │
│ SubmittedDate       │
│ LastUpdatedDate     │
│ TotalAmount         │
│ FraudScore          │
│ ApprovalScore       │
│ AssignedSpecialistId│
└─────────────────────┘
         │
         ├─────────────────────┐
         │                     │
         ▼                     ▼
┌─────────────────────┐  ┌──────────────────┐
│    Documents        │  │    Decisions     │
├─────────────────────┤  ├──────────────────┤
│ DocumentId (PK)     │  │ DecisionId (PK)  │
│ ClaimId (FK)        │  │ ClaimId (FK)     │
│ DocumentType        │  │ Status           │
│ ExtractedText       │  │ DecisionDate     │
│ OcrConfidence       │  │ Reason           │
│ OcrStatus           │  │ ReviewerId       │
│ UploadedDate        │  │ CreatedDate      │
│ BlobUri             │  └──────────────────┘
└─────────────────────┘
         │
         ▼
┌──────────────────────┐
│   Notifications      │
├──────────────────────┤
│ NotificationId (PK)  │
│ ClaimId (FK)         │
│ NotificationType     │
│ SentDate             │
│ RecipientEmail       │
│ Status               │
│ ErrorMessage         │
└──────────────────────┘
```

### Data Models (C# Classes)

#### Claim Entity
```csharp
public class Claim
{
    public Guid ClaimId { get; set; }
    public string PolicyId { get; set; }
    public string ClaimantId { get; set; }
    public ClaimStatus Status { get; set; }
    public DateTime SubmittedDate { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal? FraudScore { get; set; }      // 0.0 - 1.0
    public decimal? ApprovalScore { get; set; }   // 0.0 - 1.0
    public string? AssignedSpecialistId { get; set; }
    
    // Navigation Properties
    public virtual ICollection<Document> Documents { get; set; }
    public virtual ICollection<Decision> Decisions { get; set; }
    public virtual ICollection<Notification> Notifications { get; set; }
}
```

#### Document Entity
```csharp
public class Document
{
    public Guid DocumentId { get; set; }
    public Guid ClaimId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string BlobUri { get; set; }
    public string? ExtractedText { get; set; }    // OCR Output
    public decimal? OcrConfidence { get; set; }   // 0.0 - 1.0
    public OcrStatus OcrStatus { get; set; }
    public DateTime UploadedDate { get; set; }
    
    // Navigation Property
    public virtual Claim Claim { get; set; }
}
```

#### Decision Entity
```csharp
public class Decision
{
    public Guid DecisionId { get; set; }
    public Guid ClaimId { get; set; }
    public DecisionStatus Status { get; set; }    // Approved, Rejected, PendingReview
    public DateTime DecisionDate { get; set; }
    public string? Reason { get; set; }
    public string? ReviewerId { get; set; }       // "AI_SYSTEM" for auto decisions
    
    // Navigation Property
    public virtual Claim Claim { get; set; }
}
```

#### Notification Entity
```csharp
public class Notification
{
    public Guid NotificationId { get; set; }
    public Guid ClaimId { get; set; }
    public NotificationType NotificationType { get; set; }
    public DateTime SentDate { get; set; }
    public string RecipientEmail { get; set; }
    public NotificationStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Navigation Property
    public virtual Claim Claim { get; set; }
}
```

---

## Processing Pipeline

### Visual Pipeline Representation

```
INPUT: Claim with Documents
  │
  ▼
┌─────────────────────────────────────────┐
│ STEP 1: OCR & TEXT EXTRACTION           │
├─────────────────────────────────────────┤
│ For each document:                       │
│ 1. Load from blob storage                │
│ 2. Run Tesseract OCR                     │
│ 3. Extract text                          │
│ 4. Calculate confidence (0.0-1.0)        │
│ Output: ExtractedText, OcrConfidence     │
└─────────────────────────────────────────┘
  │ Success?
  ├─ Yes ──────────────────┐
  │                        │
  ▼                        ▼
┌─────────────────────┐  ┌────────────────────┐
│ STEP 2: DOCUMENT    │  │ STEP 2: LOG FAILURE│
│ CLASSIFICATION      │  └────────────────────┘
├─────────────────────┤
│ Analyze text        │
│ Identify type:      │
│ • Medical Report    │
│ • Police Report     │
│ • Invoice           │
│ • Evidence/Photo    │
└─────────────────────┘
  │
  ▼
┌─────────────────────────────────────────┐
│ STEP 3: RULES ENGINE VALIDATION         │
├─────────────────────────────────────────┤
│ 1. Check policy validity                │
│ 2. Verify coverage                      │
│ 3. Check amount limits                  │
└─────────────────────────────────────────┘
  │ Valid?
  ├─ No ──────────────────┐
  │                       ▼
  │              ┌──────────────────┐
  │              │ REJECT CLAIM     │
  │              │ (No ML scoring)  │
  │              └──────────────────┘
  │
  Yes
  │
  ▼
┌─────────────────────────────────────────┐
│ STEP 4: ML FRAUD DETECTION              │
├─────────────────────────────────────────┤
│ 1. Extract features from claim          │
│ 2. Load ML.NET model                    │
│ 3. Calculate:                           │
│    • FraudScore (0.0-1.0)               │
│    • ApprovalScore (0.0-1.0)            │
└─────────────────────────────────────────┘
  │
  ▼
┌─────────────────────────────────────────┐
│ STEP 5: DECISION LOGIC                  │
├─────────────────────────────────────────┤
│ FraudScore > 0.7 && ApprovalScore < 0.5 │
│ → REJECT                                │
│                                         │
│ FraudScore < 0.4 && ApprovalScore > 0.75│
│ → AUTO-APPROVE                          │
│                                         │
│ Otherwise                               │
│ → MANUAL REVIEW                         │
└─────────────────────────────────────────┘
  │
  ▼
┌─────────────────────────────────────────┐
│ STEP 6: SAVE & NOTIFY                   │
├─────────────────────────────────────────┤
│ 1. Update claim status                  │
│ 2. Create decision record               │
│ 3. Send notification email              │
│ 4. Return processing result             │
└─────────────────────────────────────────┘
  │
  ▼
OUTPUT: ClaimProcessingResult
  ├─ claimId
  ├─ finalDecision
  ├─ decisionReason
  ├─ ocrResults[]
  ├─ rulesValidation
  ├─ mlScoring
  └─ processingTimeMs
```

---

## Decision Logic Flow

### Decision Tree

```
                    START
                      │
         ┌────────────┴────────────┐
         │                         │
         ▼                         ▼
  FraudScore > 0.7       ApprovalScore > 0.75
         │                         │
    NO  │  YES                     │
        │   │            ┌─────────┤
        │   │            │         │
        ▼   ▼            ▼         ▼
    Check  HIGH      Check    Check
    Approval FRAUD   Fraud    Fraud
    Score   RISK     Score    Score
        │   │            │         │
        │   │      LOW   │      NO │ YES
        │   │    FRAUD   │        │
        │   │      │     │        │
        │   ▼      │     ▼        │
        │   └──────┼──>REJECT     ▼
        │          │              │
        ▼          │           Approval
    Check       Approval       > 0.75
    Score       < 0.5             │
        │          │              │
    NO │  YES      │              │
    ▼  │           ▼              ▼
   MANUAL REJECT                AUTO
   REVIEW                       APPROVE
        │          │              │
        └──────────┴──────────────┘
                   │
                   ▼
              Decision Made
```

### Decision Rules (Pseudocode)

```csharp
public string DetermineDecision(decimal fraudScore, decimal approvalScore)
{
    // Rule 1: High fraud risk → Reject
    if (fraudScore > 0.7m && approvalScore < 0.5m)
        return "Reject";
    
    // Rule 2: Low fraud, high approval → Auto-approve
    if (fraudScore < 0.4m && approvalScore > 0.75m)
        return "AutoApprove";
    
    // Rule 3: Everything else → Manual review
    return "ManualReview";
}
```

---

## Data Structures Reference

### Request DTOs

#### ClaimSubmissionDto
```csharp
{
  "policyId": "POL-2025-001",
  "claimantId": "CLAIMANT-001",
  "totalAmount": 5000.50,
  "documents": [
    {
      "documentType": "MedicalReport",
      "fileName": "hospital_bill.pdf",
      "base64Content": "..."
    }
  ]
}
```

#### AddDocumentRequest
```csharp
{
  "documentType": "MedicalReport",
  "filePath": "/uploads/hospital_bill.pdf"
}
```

#### UpdateClaimStatusRequest
```csharp
{
  "status": "Approved",
  "specialistId": "SP-001"
}
```

---

### Response DTOs

#### ClaimResponseDto
```csharp
{
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Submitted",
  "submittedDate": "2025-12-29T10:30:00Z",
  "message": "Claim submitted successfully"
}
```

#### ClaimStatusDto
```csharp
{
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Approved",
  "submittedDate": "2025-12-29T10:30:00Z",
  "lastUpdatedDate": "2025-12-29T10:35:00Z",
  "totalAmount": 5000.50,
  "fraudScore": 0.25,
  "approvalScore": 0.82,
  "documents": [...],
  "decisions": [...]
}
```

#### ClaimProcessingResult
```csharp
{
  "claimId": "550e8400-e29b-41d4-a716-446655440000",
  "success": true,
  "finalDecision": "AutoApprove",
  "decisionReason": "Auto-approved: Low fraud risk (0.25), high approval score (0.82)",
  "processingTimeMs": 5432.50,
  "ocrResults": [
    {
      "documentId": "...",
      "extractedText": "...",
      "confidence": 0.95,
      "classifiedType": "MedicalReport",
      "success": true
    }
  ],
  "rulesValidation": {
    "isValid": true,
    "reason": "Policy is valid and covers this claim type",
    "rulesChecked": ["PolicyLimit", "PolicyValidity", "CoverageCheck"]
  },
  "mlScoring": {
    "fraudScore": 0.25,
    "approvalScore": 0.82,
    "fraudRiskLevel": "Low"
  }
}
```

---

## Execution Sequence Diagram

### Claim Submission & Processing Sequence

```
User                Controller           Service           OCR Service         ML Service          Database           Notification
  │                    │                   │                  │                   │                   │                    │
  ├─ POST /api/claims  │                   │                  │                   │                   │                    │
  ├───────────────────>│                   │                  │                   │                   │                    │
  │                    │                   │                  │                   │                   │                    │
  │                    ├─ SubmitClaim()    │                  │                   │                   │                    │
  │                    ├──────────────────>│                  │                   │                   │                    │
  │                    │                   │                  │                   │                   │                    │
  │                    │                   ├─ Create Claim ──────────────────────────────────────────>│                    │
  │                    │                   ├─ Save to DB                                               │                    │
  │                    │                   │<─ Claim saved with ID ────────────────────────────────────┤                    │
  │                    │                   │                  │                   │                   │                    │
  │                    │                   ├─────────────────────────────────────────────────────────────────> Send notification
  │                    │                   │<──────────────────────────────────────────────────────────────── Email sent
  │                    │                   │                  │                   │                   │                    │
  │<── 201 Created ────┤<─ ClaimResponseDto┤                  │                   │                   │                    │
  │    with ClaimId    │<────────────────────                  │                   │                   │                    │
  │                    │                   │                  │                   │                   │                    │
  │                    │                   │                  │                   │                   │                    │
  ├─ POST /api/claims/{id}/process       │                  │                   │                   │                    │
  ├───────────────────>│                   │                  │                   │                   │                    │
  │                    │                   │                  │                   │                   │                    │
  │                    ├─ ProcessClaim() │                  │                   │                   │                    │
  │                    ├──────────────────>│                  │                   │                   │                    │
  │                    │                   ├─ Retrieve Claim ────────────────────────────────────────>│                    │
  │                    │                   │<─ Claim + Documents ──────────────────────────────────────┤                    │
  │                    │                   │                  │                   │                   │                    │
  │                    │                   ├─ For each Doc: ProcessDocumentAsync() │                   │                    │
  │                    │                   ├─────────────────>│                   │                   │                    │
  │                    │                   │<─ Extract text & confidence ────────│                   │                    │
  │                    │                   │                  │                   │                   │                    │
  │                    │                   ├─ ClassifyDocument()               │                    │                    │
  │                    │                   ├─────────────────>│                   │                   │                    │
  │                    │                   │<─ Document Type ──────────────────│                    │                    │
  │                    │                   │                  │                   │                   │                    │
  │                    │                   ├─ Update Documents ───────────────────────────────────────>│                    │
  │                    │                   │                  │                   │                   │                    │
  │                    │                   ├─ ValidatePolicyAsync()           │                    │                    │
  │                    │                   ├──────────────────────────────────────────────────────────>│ Check Policy       │
  │                    │                   │<────────────────────────────────────────────────────────── Is Valid?         │
  │                    │                   │                  │                   │                   │                    │
  │                    │                   ├─ ScoreClaimAsync() ────────────────────────────────────>│                    │
  │                    │                   │                  │                   ├─ Predict fraud/approval scores         │
  │                    │                   │<──────────── fraudScore, approvalScore ────────────────┤                    │
  │                    │                   │                  │                   │                   │                    │
  │                    │                   ├─ DetermineDecisionAsync()         │                    │                    │
  │                    │                   ├───────────────────────────────────────────────────────>│ Scores              │
  │                    │                   │<─────────────────────────────────────────────────────── Decision            │
  │                    │                   │                  │                   │                   │                    │
  │                    │                   ├─ Create Decision ────────────────────────────────────────>│                    │
  │                    │                   ├─ Update Claim Status ────────────────────────────────────>│                    │
  │                    │                   │<─ Saved ──────────────────────────────────────────────────┤                    │
  │                    │                   │                  │                   │                   │                    │
  │                    │                   ├───────────────────────────────────────────────────────────────────> Send Decision notification
  │                    │                   │                  │                   │                   │                    │
  │<── 200 OK ─────────┤<─ ClaimProcessingResult               │                   │                   │                    │
  │                    │<────────────────────                  │                   │                   │                    │
  │                    │                   │                  │                   │                   │                    │
```

---

## Quick Reference: Key Files & Methods

### Controllers
- **[Claims.Api/Controllers/ClaimsController.cs](src/Claims.Api/Controllers/ClaimsController.cs)** - REST API endpoints
  - `SubmitClaim()` - POST /api/claims
  - `ProcessClaim()` - POST /api/claims/{id}/process
  - `GetClaimStatus()` - GET /api/claims/{id}/status

### Services
- **[Claims.Services/Implementations/ClaimsService.cs](src/Claims.Services/Implementations/ClaimsService.cs)** - Main orchestrator
  - `SubmitClaimAsync()` - Claim submission
  - `ProcessClaimAsync()` - Complete AI pipeline

- **[Claims.Services/Implementations/OcrService.cs](src/Claims.Services/Implementations/OcrService.cs)** - Tesseract OCR
  - `ProcessDocumentAsync()` - Extract text

- **[Claims.Services/Implementations/MlScoringService.cs](src/Claims.Services/Implementations/MlScoringService.cs)** - Fraud detection
  - `ScoreClaimAsync()` - Generate fraud/approval scores
  - `DetermineDecisionAsync()` - Apply decision logic

- **[Claims.Services/Implementations/RulesEngineService.cs](src/Claims.Services/Implementations/RulesEngineService.cs)** - Business rules
  - `ValidatePolicyAsync()` - Validate against policies

### Domain Models
- **[Claims.Domain/Entities/Claim.cs](src/Claims.Domain/Entities/Claim.cs)** - Main claim entity
- **[Claims.Domain/Entities/Document.cs](src/Claims.Domain/Entities/Document.cs)** - Document entity
- **[Claims.Domain/DTOs/](src/Claims.Domain/DTOs/)** - Request/response DTOs

### Database
- **[Claims.Infrastructure/Data/ClaimsDbContext.cs](src/Claims.Infrastructure/Data/ClaimsDbContext.cs)** - EF Core context

---

## Summary

This Claims Validation System processes insurance claims through an **automated 6-step pipeline**:

1. **OCR Extraction** - Tesseract extracts text from documents
2. **Document Classification** - Identifies document types
3. **Rules Validation** - Checks business policies
4. **Fraud Detection** - ML model scores fraud risk
5. **Decision Logic** - Determines approval/rejection/review
6. **Notification** - Sends email to claimant

All processing is **logged, tracked, and persisted** in the database for audit trails and manual review capability.
