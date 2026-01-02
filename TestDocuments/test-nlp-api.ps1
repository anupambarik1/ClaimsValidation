# PowerShell Script to Test Claims API with NLP Integration
# Run this script to test the complete flow: Submit -> Add Document -> Process

param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$DocumentPath = "C:\Hackathon Projects\TestDocuments\sample-claim-document.txt"
)

$ErrorActionPreference = "Stop"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Claims API - NLP Integration Test" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Submit a Claim
Write-Host "[STEP 1] Submitting a new claim..." -ForegroundColor Yellow

$claimPayload = @{
    policyId = "POL-2024-567890"
    claimantId = "CLMT-JOHN-DAVIS-$(Get-Random)"
    totalAmount = 8850.00
} | ConvertTo-Json

try {
    $submitResponse = Invoke-RestMethod `
        -Uri "$BaseUrl/api/claims" `
        -Method POST `
        -ContentType "application/json" `
        -Body $claimPayload
    
    $claimId = $submitResponse.claimId
    Write-Host "✓ Claim submitted successfully" -ForegroundColor Green
    Write-Host "  Claim ID: $claimId" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "✗ Failed to submit claim: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Add Document to Claim
Write-Host "[STEP 2] Adding sample document to claim..." -ForegroundColor Yellow

if (-not (Test-Path $DocumentPath)) {
    Write-Host "✗ Document not found at: $DocumentPath" -ForegroundColor Red
    Write-Host "  Please check the file path" -ForegroundColor Red
    exit 1
}

$documentPayload = @{
    filePath = $DocumentPath
    documentType = "AccidentReport"
} | ConvertTo-Json

try {
    $docResponse = Invoke-RestMethod `
        -Uri "$BaseUrl/api/claims/$claimId/documents" `
        -Method POST `
        -ContentType "application/json" `
        -Body $documentPayload
    
    Write-Host "✓ Document added successfully" -ForegroundColor Green
    Write-Host "  Document ID: $($docResponse.documentId)" -ForegroundColor Green
    Write-Host "  File: $($docResponse.documentType)" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "✗ Failed to add document: $_" -ForegroundColor Red
    exit 1
}

# Step 3: Process Claim (Triggers NLP)
Write-Host "[STEP 3] Processing claim (NLP Analysis in progress)..." -ForegroundColor Yellow
Write-Host "  - OCR text extraction" -ForegroundColor Gray
Write-Host "  - NLP Bedrock summarization" -ForegroundColor Gray
Write-Host "  - Fraud analysis (Bedrock + Comprehend)" -ForegroundColor Gray
Write-Host "  - Entity extraction" -ForegroundColor Gray
Write-Host "  - ML scoring + NLP combination" -ForegroundColor Gray
Write-Host ""

try {
    $processResponse = Invoke-RestMethod `
        -Uri "$BaseUrl/api/claims/$claimId/process" `
        -Method POST `
        -ContentType "application/json"
    
    if ($processResponse.success) {
        Write-Host "✓ Claim processed successfully" -ForegroundColor Green
        Write-Host ""
        
        # Display Results
        Write-Host "======================================" -ForegroundColor Cyan
        Write-Host "RESULTS" -ForegroundColor Cyan
        Write-Host "======================================" -ForegroundColor Cyan
        Write-Host ""
        
        Write-Host "Final Decision: $($processResponse.finalDecision)" -ForegroundColor Yellow
        Write-Host "Decision Reason: $($processResponse.decisionReason)" -ForegroundColor Gray
        Write-Host ""
        
        # NLP Analysis Results
        if ($processResponse.nlpAnalysis) {
            Write-Host "NLP ANALYSIS RESULTS:" -ForegroundColor Cyan
            Write-Host "  Summary: $($processResponse.nlpAnalysis.summary)" -ForegroundColor Gray
            Write-Host "  Fraud Risk Score (NLP): $([Math]::Round($processResponse.nlpAnalysis.fraudRiskScore, 2))" -ForegroundColor Gray
            Write-Host "  Claim Type: $($processResponse.nlpAnalysis.claimType)" -ForegroundColor Gray
            Write-Host "  Detected Entities: $($processResponse.nlpAnalysis.detectedEntities.Substring(0, [Math]::Min(100, $processResponse.nlpAnalysis.detectedEntities.Length)))..." -ForegroundColor Gray
            Write-Host ""
        }
        
        # ML Scoring Results
        if ($processResponse.mlScoring) {
            Write-Host "ML SCORING RESULTS:" -ForegroundColor Cyan
            Write-Host "  Fraud Score (Combined 60% ML + 40% NLP): $([Math]::Round($processResponse.mlScoring.fraudScore, 2))" -ForegroundColor Yellow
            Write-Host "  Approval Score: $([Math]::Round($processResponse.mlScoring.approvalScore, 2))" -ForegroundColor Gray
            Write-Host "  Fraud Risk Level: $($processResponse.mlScoring.fraudRiskLevel)" -ForegroundColor Gray
            Write-Host ""
        }
        
        # OCR Results
        if ($processResponse.ocrResults.Count -gt 0) {
            Write-Host "OCR RESULTS:" -ForegroundColor Cyan
            foreach ($ocr in $processResponse.ocrResults) {
                Write-Host "  Document Type: $($ocr.classifiedType)" -ForegroundColor Gray
                Write-Host "  Confidence: $([Math]::Round($ocr.confidence * 100, 1))%" -ForegroundColor Gray
                Write-Host "  Text Extracted: $(if ($ocr.extractedText) { $ocr.extractedText.Substring(0, [Math]::Min(50, $ocr.extractedText.Length)) + '...' } else { 'None' })" -ForegroundColor Gray
            }
            Write-Host ""
        }
        
        # Processing Performance
        Write-Host "PERFORMANCE:" -ForegroundColor Cyan
        Write-Host "  Total Processing Time: $([Math]::Round($processResponse.processingTimeMs, 1)) ms" -ForegroundColor Gray
        Write-Host ""
        
    } else {
        Write-Host "✗ Claim processing failed: $($processResponse.errorMessage)" -ForegroundColor Red
        Write-Host ""
    }
    
} catch {
    Write-Host "✗ Failed to process claim: $_" -ForegroundColor Red
    exit 1
}

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Test Complete!" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Check application logs for NLP processing steps" -ForegroundColor Gray
Write-Host "2. Verify fraud scores are in valid range (0.0-1.0)" -ForegroundColor Gray
Write-Host "3. Review the NLP summary for accuracy" -ForegroundColor Gray
Write-Host "4. Confirm final decision is appropriate for risk level" -ForegroundColor Gray
Write-Host ""
