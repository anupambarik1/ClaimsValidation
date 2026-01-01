# Quick API Test Script

# Test the working POC by submitting a claim

$apiUrl = "http://localhost:5159/api/claims"

$claim = @{
    policyId = "POL-TEST-001"
    claimantId = "demo.user@example.com"
    totalAmount = 1500.00
    documents = @(
        @{
            documentType = "Invoice"
            fileName = "invoice.pdf"
            base64Content = ""
        }
    )
} | ConvertTo-Json

Write-Host "Submitting test claim..." -ForegroundColor Cyan
Write-Host $claim -ForegroundColor Gray

try {
    $response = Invoke-RestMethod -Uri $apiUrl -Method Post -Body $claim -ContentType "application/json"
    
    Write-Host "`n✅ Claim submitted successfully!" -ForegroundColor Green
    Write-Host "Claim ID: $($response.claimId)" -ForegroundColor Yellow
    Write-Host "Status: $($response.status)" -ForegroundColor Yellow
    Write-Host "Fraud Score: $($response.fraudScore)" -ForegroundColor Yellow
    Write-Host "Approval Score: $($response.approvalScore)" -ForegroundColor Yellow
    
    # Get claim status
    Write-Host "`nFetching claim status..." -ForegroundColor Cyan
    $statusUrl = "http://localhost:5159/api/claims/$($response.claimId)/status"
    $status = Invoke-RestMethod -Uri $statusUrl -Method Get
    
    Write-Host "`nClaim Status Details:" -ForegroundColor Green
    $status | ConvertTo-Json | Write-Host -ForegroundColor Gray
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Make sure the API is running at http://localhost:5159" -ForegroundColor Yellow
}
