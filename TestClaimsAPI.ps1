# Claims Validation API Test Script
# Run this script to test all API endpoints

$baseUrl = "http://localhost:5159"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Claims Validation API Test Suite" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Health Check
Write-Host "1. Testing Health Endpoint..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get
    Write-Host "   Status: $($health.status)" -ForegroundColor Green
    Write-Host "   Service: $($health.service)" -ForegroundColor Green
    Write-Host "   Features:" -ForegroundColor Green
    foreach ($feature in $health.features) {
        Write-Host "     - $feature" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 2: API Info
Write-Host "2. Testing API Info Endpoint..." -ForegroundColor Yellow
try {
    $info = Invoke-RestMethod -Uri "$baseUrl/api" -Method Get
    Write-Host "   Name: $($info.name)" -ForegroundColor Green
    Write-Host "   Version: $($info.version)" -ForegroundColor Green
} catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 3: Submit a Claim
Write-Host "3. Submitting a Test Claim..." -ForegroundColor Yellow
$claimPayload = @{
    policyId = "PREM-12345-6789"
    claimantId = "test.user@example.com"
    totalAmount = 2500.00
    description = "Test claim for medical expenses"
} | ConvertTo-Json

try {
    $submitResult = Invoke-RestMethod -Uri "$baseUrl/api/claims" -Method Post -Body $claimPayload -ContentType "application/json"
    Write-Host "   Claim ID: $($submitResult.claimId)" -ForegroundColor Green
    Write-Host "   Status: $($submitResult.status)" -ForegroundColor Green
    Write-Host "   Message: $($submitResult.message)" -ForegroundColor Green
    $claimId = $submitResult.claimId
} catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
    $claimId = $null
}
Write-Host ""

# Test 4: Get Claim Status
if ($claimId) {
    Write-Host "4. Getting Claim Status..." -ForegroundColor Yellow
    try {
        $status = Invoke-RestMethod -Uri "$baseUrl/api/claims/$claimId/status" -Method Get
        Write-Host "   Status: $($status.status)" -ForegroundColor Green
        Write-Host "   Submitted: $($status.submittedDate)" -ForegroundColor Green
    } catch {
        Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
    
    # Test 5: Process Claim through AI Pipeline
    Write-Host "5. Processing Claim through AI Pipeline..." -ForegroundColor Yellow
    Write-Host "   (This runs OCR, ML Fraud Detection, Rules Validation)" -ForegroundColor Gray
    try {
        $processResult = Invoke-RestMethod -Uri "$baseUrl/api/claims/$claimId/process" -Method Post
        Write-Host "   Success: $($processResult.success)" -ForegroundColor Green
        Write-Host "   Final Decision: $($processResult.finalDecision)" -ForegroundColor $(if ($processResult.finalDecision -eq "AutoApprove") { "Green" } elseif ($processResult.finalDecision -eq "Reject") { "Red" } else { "Yellow" })
        Write-Host "   Decision Reason: $($processResult.decisionReason)" -ForegroundColor Gray
        
        if ($processResult.mlScoring) {
            Write-Host "   ML Scores:" -ForegroundColor Cyan
            Write-Host "     Fraud Score: $([math]::Round($processResult.mlScoring.fraudScore * 100, 2))%" -ForegroundColor Gray
            Write-Host "     Approval Score: $([math]::Round($processResult.mlScoring.approvalScore * 100, 2))%" -ForegroundColor Gray
            Write-Host "     Risk Level: $($processResult.mlScoring.fraudRiskLevel)" -ForegroundColor Gray
        }
        
        if ($processResult.rulesValidation) {
            Write-Host "   Rules Validation:" -ForegroundColor Cyan
            Write-Host "     Valid: $($processResult.rulesValidation.isValid)" -ForegroundColor Gray
        }
        
        Write-Host "   Processing Time: $([math]::Round($processResult.processingTimeMs, 2))ms" -ForegroundColor Gray
    } catch {
        Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
}

# Test 6: Submit and Process in One Call (High Amount - should trigger manual review or rejection)
Write-Host "6. Testing Submit-and-Process (High Risk Claim)..." -ForegroundColor Yellow
$highRiskPayload = @{
    policyId = "BASIC-00001-0001"
    claimantId = "suspicious.user@example.com"
    totalAmount = 9500.00
    description = "Large claim from new user"
} | ConvertTo-Json

try {
    $result = Invoke-RestMethod -Uri "$baseUrl/api/claims/submit-and-process" -Method Post -Body $highRiskPayload -ContentType "application/json"
    Write-Host "   Claim ID: $($result.claimId)" -ForegroundColor Green
    Write-Host "   Success: $($result.success)" -ForegroundColor Green
    Write-Host "   Final Decision: $($result.finalDecision)" -ForegroundColor $(if ($result.finalDecision -eq "AutoApprove") { "Green" } elseif ($result.finalDecision -eq "Reject") { "Red" } else { "Yellow" })
    Write-Host "   Decision Reason: $($result.decisionReason)" -ForegroundColor Gray
    if ($result.mlScoring) {
        Write-Host "   Fraud Score: $([math]::Round($result.mlScoring.fraudScore * 100, 2))%" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 7: Submit Low Risk Claim (should auto-approve)
Write-Host "7. Testing Submit-and-Process (Low Risk Claim)..." -ForegroundColor Yellow
$lowRiskPayload = @{
    policyId = "PREM-99999-0001"
    claimantId = "trusted.customer@example.com"
    totalAmount = 350.00
    description = "Small claim with premium policy"
} | ConvertTo-Json

try {
    $result = Invoke-RestMethod -Uri "$baseUrl/api/claims/submit-and-process" -Method Post -Body $lowRiskPayload -ContentType "application/json"
    Write-Host "   Claim ID: $($result.claimId)" -ForegroundColor Green
    Write-Host "   Success: $($result.success)" -ForegroundColor Green
    Write-Host "   Final Decision: $($result.finalDecision)" -ForegroundColor $(if ($result.finalDecision -eq "AutoApprove") { "Green" } elseif ($result.finalDecision -eq "Reject") { "Red" } else { "Yellow" })
    Write-Host "   Decision Reason: $($result.decisionReason)" -ForegroundColor Gray
    if ($result.mlScoring) {
        Write-Host "   Fraud Score: $([math]::Round($result.mlScoring.fraudScore * 100, 2))%" -ForegroundColor Gray
        Write-Host "   Approval Score: $([math]::Round($result.mlScoring.approvalScore * 100, 2))%" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ERROR: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Test Suite Complete!" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
