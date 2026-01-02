# PowerShell script to create a sample claims PDF document for testing
# This script uses Windows native PDF printing capabilities

param(
    [string]$OutputPath = ".\sample_claim_document.pdf"
)

# HTML content for the claim document
$htmlContent = @"
<!DOCTYPE html>
<html>
<head>
    <title>Insurance Claim Form</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; }
        h1 { color: #333; border-bottom: 2px solid #333; padding-bottom: 10px; }
        .section { margin: 20px 0; }
        .section-title { font-weight: bold; color: #0066cc; margin-top: 15px; }
        table { width: 100%; border-collapse: collapse; }
        td { padding: 8px; border: 1px solid #ddd; }
        .label { font-weight: bold; width: 30%; }
        .value { width: 70%; }
    </style>
</head>
<body>
    <h1>Insurance Claim Form</h1>
    
    <div class="section">
        <div class="section-title">Claim Information</div>
        <table>
            <tr>
                <td class="label">Claim Number:</td>
                <td class="value">CLM-2024-001234</td>
            </tr>
            <tr>
                <td class="label">Date of Loss:</td>
                <td class="value">January 15, 2024</td>
            </tr>
            <tr>
                <td class="label">Claim Type:</td>
                <td class="value">Auto Accident</td>
            </tr>
            <tr>
                <td class="label">Claim Status:</td>
                <td class="value">New</td>
            </tr>
        </table>
    </div>

    <div class="section">
        <div class="section-title">Policyholder Information</div>
        <table>
            <tr>
                <td class="label">Name:</td>
                <td class="value">John Michael Davis</td>
            </tr>
            <tr>
                <td class="label">Policy Number:</td>
                <td class="value">POL-2024-567890</td>
            </tr>
            <tr>
                <td class="label">Phone:</td>
                <td class="value">(555) 123-4567</td>
            </tr>
            <tr>
                <td class="label">Email:</td>
                <td class="value">john.davis@email.com</td>
            </tr>
        </table>
    </div>

    <div class="section">
        <div class="section-title">Incident Details</div>
        <table>
            <tr>
                <td class="label">Location:</td>
                <td class="value">Intersection of Main Street and Oak Avenue, Springfield, IL 62701</td>
            </tr>
            <tr>
                <td class="label">Time of Incident:</td>
                <td class="value">2:30 PM</td>
            </tr>
            <tr>
                <td class="label">Vehicle Year/Make/Model:</td>
                <td class="value">2023 Honda Accord</td>
            </tr>
            <tr>
                <td class="label">Vehicle Identification Number:</td>
                <td class="value">1HGCV1F32PA123456</td>
            </tr>
        </table>
    </div>

    <div class="section">
        <div class="section-title">Damage Description</div>
        <p>
            The vehicle was involved in a two-vehicle collision at the intersection of Main Street 
            and Oak Avenue. The other vehicle ran a red light and struck the passenger side of my vehicle. 
            The impact caused significant damage to the right side of the vehicle, including the front 
            passenger door, fender, and quarter panel. The airbags deployed and the vehicle was not drivable 
            after the incident. The other driver's insurance information has been obtained and a police 
            report was filed (Report #2024-45678).
        </p>
    </div>

    <div class="section">
        <div class="section-title">Damage Claim Amount</div>
        <table>
            <tr>
                <td class="label">Estimated Repair Cost:</td>
                <td class="value">$8,500.00</td>
            </tr>
            <tr>
                <td class="label">Deductible:</td>
                <td class="value">$500.00</td>
            </tr>
            <tr>
                <td class="label">Rental Car Expense (5 days @ $75/day):</td>
                <td class="value">$375.00</td>
            </tr>
            <tr>
                <td class="label"><b>Total Claim Amount:</b></td>
                <td class="value"><b>$8,375.00</b></td>
            </tr>
        </table>
    </div>

    <div class="section">
        <div class="section-title">Supporting Documentation</div>
        <ul>
            <li>Police Report #2024-45678</li>
            <li>Repair Estimate from ABC Auto Body Shop</li>
            <li>Photos of Vehicle Damage (attached)</li>
            <li>Other Driver's Insurance Information</li>
            <li>Medical Records (minor injuries treated at urgent care)</li>
        </ul>
    </div>

    <div class="section">
        <div class="section-title">Claimant Declaration</div>
        <p>
            I certify that the information provided in this claim is true and accurate to the best of my knowledge. 
            I understand that providing false information may result in denial of the claim and legal action.
        </p>
        <p>
            <b>Signature:</b> John Michael Davis<br/>
            <b>Date:</b> January 20, 2024
        </p>
    </div>

    <hr/>
    <p style="font-size: 10px; color: #666;">
        This is a sample claim document for testing purposes. Generated on $(Get-Date -Format 'MM/dd/yyyy HH:mm:ss')
    </p>
</body>
</html>
"@

# Save HTML to temporary file
$htmlFile = [System.IO.Path]::GetTempFileName() -replace '\.tmp$', '.html'
$htmlContent | Out-File -FilePath $htmlFile -Encoding UTF8

Write-Host "HTML file created at: $htmlFile"
Write-Host "To convert to PDF, you can use one of these methods:"
Write-Host ""
Write-Host "METHOD 1: Using Microsoft Word (if installed)"
Write-Host "  `$word = New-Object -ComObject Word.Application"
Write-Host "  `$doc = `$word.Documents.Open('$htmlFile')"
Write-Host "  `$doc.SaveAs('$OutputPath', 17)"
Write-Host ""
Write-Host "METHOD 2: Using Google Chrome/Edge (command line)"
Write-Host "  chrome --headless --disable-gpu --print-to-pdf='$OutputPath' '$htmlFile'"
Write-Host ""
Write-Host "METHOD 3: Online converter"
Write-Host "  1. Go to https://html2pdf.com or similar service"
Write-Host "  2. Upload the HTML file: $htmlFile"
Write-Host "  3. Convert to PDF and save as: $OutputPath"
Write-Host ""
Write-Host "The HTML file is ready and can be manually converted to PDF."
Write-Host "HTML File: $htmlFile"
