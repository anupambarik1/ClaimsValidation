@"
# Sample Claims Document - For Testing NLP Integration
## Created: January 2, 2026

---

## INSURANCE CLAIM FORM

### Claim Information
- **Claim Number**: CLM-2024-001234
- **Date of Loss**: January 15, 2024
- **Claim Type**: Auto Accident
- **Claim Status**: New Submission

### Policyholder Information
- **Name**: John Michael Davis
- **Policy Number**: POL-2024-567890
- **Phone**: (555) 123-4567
- **Email**: john.davis@email.com
- **Address**: 123 Maple Drive, Springfield, IL 62701

### Incident Details
- **Location**: Intersection of Main Street and Oak Avenue, Springfield, IL 62701
- **Date & Time**: January 15, 2024 at 2:30 PM
- **Vehicle**: 2023 Honda Accord
- **VIN**: 1HGCV1F32PA123456
- **License Plate**: ABC-1234

### Accident Description

On January 15, 2024, at approximately 2:30 PM, I was driving my 2023 Honda Accord northbound on Main Street approaching the intersection with Oak Avenue. The traffic light was green in my direction.

As I entered the intersection, another vehicle (2022 Toyota Camry, License Plate XYZ-5678) ran the red light and struck the passenger side of my vehicle with significant force.

The impact was severe:
- The front right passenger door was crushed inward
- The right front fender sustained major damage
- The right quarter panel is dented and misaligned
- The airbags deployed on the passenger side
- The vehicle sustained frame damage and is currently not drivable

I immediately pulled over and called 911. Emergency medical services arrived and provided assistance. Police arrived and filed an incident report (Report #2024-45678).

### Injuries
Minor injuries sustained:
- Chest contusion from airbag deployment
- Neck strain
- Left arm abrasion
- Treated at Springfield Urgent Care on January 15, 2024

### Damages and Costs

**Vehicle Repair Estimate**:
- ABC Auto Body Shop provided estimate: $8,500.00
- Parts: $5,200.00
- Labor: $3,000.00
- Paint and finishing: $300.00

**Additional Expenses**:
- Rental vehicle (5 days @ $75/day): $375.00
- Urgent care medical bill: $450.00
- Police report copy: $25.00

**Total Claim Amount**: $9,350.00

**Insurance Deductible**: $500.00

**Net Claim Request**: $8,850.00

### Supporting Documentation Attached

1. Police Report #2024-45678 (official incident report)
2. Repair Estimate from ABC Auto Body Shop (itemized)
3. Photos of vehicle damage (12 photos showing all angles)
4. Medical treatment records from Springfield Urgent Care
5. Rental car receipt and invoice
6. Other driver's insurance information and contact details
7. Witness statements (2 witnesses provided statements)
8. Photos of the accident scene

### Other Driver Information

- **Name**: Robert James Thompson
- **Vehicle**: 2022 Toyota Camry
- **License Plate**: XYZ-5678
- **Insurance**: XYZ Insurance Company
- **Policy Number**: XYZ-POL-654321
- **Insurance Adjuster**: Margaret Wilson, (555) 987-6543

### Witness Information

**Witness 1**:
- Name: Sarah Michelle Johnson
- Phone: (555) 234-5678
- Occupation: Teacher

**Witness 2**:
- Name: Robert Chen
- Phone: (555) 345-6789
- Occupation: Engineer

Both witnesses signed statements confirming that the other vehicle ran the red light.

### Declaration

I certify that the information provided in this claim is true and accurate to the best of my knowledge. I understand that providing false information may result in denial of the claim and legal prosecution under applicable state and federal laws.

The incident occurred as described, and I have provided all relevant documentation and evidence in support of this claim.

---

**Signature**: John Michael Davis
**Date**: January 20, 2024
**Claim Submission Date**: January 20, 2024

---

*This is a sample insurance claim document for testing and training purposes.*
"@ | Out-File -FilePath "C:\Hackathon Projects\TestDocuments\sample-claim-document.txt" -Encoding UTF8
Write-Host "Created sample claim document text file"
