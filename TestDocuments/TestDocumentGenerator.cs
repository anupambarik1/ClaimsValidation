using System;
using System.IO;
using System.Text;

namespace TestDocumentGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            string outputPath = args.Length > 0 ? args[0] : "sample_claim_document.txt";
            GenerateSampleClaimDocument(outputPath);
            Console.WriteLine($"Sample claim document created: {outputPath}");
        }

        static void GenerateSampleClaimDocument(string filePath)
        {
            var content = new StringBuilder();
            
            content.AppendLine("================================================================================");
            content.AppendLine("INSURANCE CLAIM FORM - SAMPLE DOCUMENT FOR TESTING");
            content.AppendLine("================================================================================");
            content.AppendLine();
            
            content.AppendLine("CLAIM INFORMATION");
            content.AppendLine("----------------");
            content.AppendLine("Claim Number: CLM-2024-001234");
            content.AppendLine("Date of Loss: January 15, 2024");
            content.AppendLine("Claim Type: Auto Accident");
            content.AppendLine("Claim Status: New Submission");
            content.AppendLine();
            
            content.AppendLine("POLICYHOLDER INFORMATION");
            content.AppendLine("------------------------");
            content.AppendLine("Name: John Michael Davis");
            content.AppendLine("Policy Number: POL-2024-567890");
            content.AppendLine("Phone: (555) 123-4567");
            content.AppendLine("Email: john.davis@email.com");
            content.AppendLine("Address: 123 Maple Drive, Springfield, IL 62701");
            content.AppendLine();
            
            content.AppendLine("INCIDENT DETAILS");
            content.AppendLine("----------------");
            content.AppendLine("Location: Intersection of Main Street and Oak Avenue, Springfield, IL 62701");
            content.AppendLine("Date & Time: January 15, 2024 at 2:30 PM");
            content.AppendLine("Vehicle: 2023 Honda Accord");
            content.AppendLine("VIN: 1HGCV1F32PA123456");
            content.AppendLine("License Plate: ABC-1234");
            content.AppendLine();
            
            content.AppendLine("ACCIDENT DESCRIPTION");
            content.AppendLine("--------------------");
            content.AppendLine("On January 15, 2024, at approximately 2:30 PM, I was driving my 2023 Honda");
            content.AppendLine("Accord northbound on Main Street approaching the intersection with Oak Avenue.");
            content.AppendLine("The traffic light was green in my direction.");
            content.AppendLine();
            content.AppendLine("As I entered the intersection, another vehicle (2022 Toyota Camry, License");
            content.AppendLine("Plate XYZ-5678) ran the red light and struck the passenger side of my vehicle");
            content.AppendLine("with significant force.");
            content.AppendLine();
            content.AppendLine("The impact was severe:");
            content.AppendLine("- The front right passenger door was crushed inward");
            content.AppendLine("- The right front fender sustained major damage");
            content.AppendLine("- The right quarter panel is dented and misaligned");
            content.AppendLine("- The airbags deployed on the passenger side");
            content.AppendLine("- The vehicle sustained frame damage and is currently not drivable");
            content.AppendLine();
            content.AppendLine("I immediately pulled over and called 911. Emergency medical services arrived");
            content.AppendLine("and provided assistance. Police arrived and filed an incident report");
            content.AppendLine("(Report #2024-45678).");
            content.AppendLine();
            
            content.AppendLine("INJURIES");
            content.AppendLine("--------");
            content.AppendLine("Minor injuries sustained:");
            content.AppendLine("- Chest contusion from airbag deployment");
            content.AppendLine("- Neck strain");
            content.AppendLine("- Left arm abrasion");
            content.AppendLine("Treated at Springfield Urgent Care on January 15, 2024");
            content.AppendLine();
            
            content.AppendLine("DAMAGES AND COSTS");
            content.AppendLine("-----------------");
            content.AppendLine("Vehicle Repair Estimate (ABC Auto Body Shop):");
            content.AppendLine("  Parts: $5,200.00");
            content.AppendLine("  Labor: $3,000.00");
            content.AppendLine("  Paint and finishing: $300.00");
            content.AppendLine("  Total: $8,500.00");
            content.AppendLine();
            content.AppendLine("Additional Expenses:");
            content.AppendLine("  Rental vehicle (5 days @ $75/day): $375.00");
            content.AppendLine("  Urgent care medical bill: $450.00");
            content.AppendLine("  Police report copy: $25.00");
            content.AppendLine();
            content.AppendLine("Total Claim Amount: $9,350.00");
            content.AppendLine("Insurance Deductible: $500.00");
            content.AppendLine("Net Claim Request: $8,850.00");
            content.AppendLine();
            
            content.AppendLine("SUPPORTING DOCUMENTATION");
            content.AppendLine("-------------------------");
            content.AppendLine("- Police Report #2024-45678 (official incident report)");
            content.AppendLine("- Repair Estimate from ABC Auto Body Shop (itemized)");
            content.AppendLine("- Photos of vehicle damage (12 photos showing all angles)");
            content.AppendLine("- Medical treatment records from Springfield Urgent Care");
            content.AppendLine("- Rental car receipt and invoice");
            content.AppendLine("- Other driver's insurance information and contact details");
            content.AppendLine("- Witness statements (2 witnesses provided statements)");
            content.AppendLine("- Photos of the accident scene");
            content.AppendLine();
            
            content.AppendLine("OTHER DRIVER INFORMATION");
            content.AppendLine("------------------------");
            content.AppendLine("Name: Robert James Thompson");
            content.AppendLine("Vehicle: 2022 Toyota Camry");
            content.AppendLine("License Plate: XYZ-5678");
            content.AppendLine("Insurance: XYZ Insurance Company");
            content.AppendLine("Policy Number: XYZ-POL-654321");
            content.AppendLine("Insurance Adjuster: Margaret Wilson, (555) 987-6543");
            content.AppendLine();
            
            content.AppendLine("WITNESS INFORMATION");
            content.AppendLine("-------------------");
            content.AppendLine("Witness 1:");
            content.AppendLine("  Name: Sarah Michelle Johnson");
            content.AppendLine("  Phone: (555) 234-5678");
            content.AppendLine("  Occupation: Teacher");
            content.AppendLine();
            content.AppendLine("Witness 2:");
            content.AppendLine("  Name: Robert Chen");
            content.AppendLine("  Phone: (555) 345-6789");
            content.AppendLine("  Occupation: Engineer");
            content.AppendLine();
            content.AppendLine("Both witnesses signed statements confirming that the other vehicle ran the");
            content.AppendLine("red light and was at fault for the accident.");
            content.AppendLine();
            
            content.AppendLine("DECLARATION");
            content.AppendLine("-----------");
            content.AppendLine("I certify that the information provided in this claim is true and accurate to");
            content.AppendLine("the best of my knowledge. I understand that providing false information may");
            content.AppendLine("result in denial of the claim and legal prosecution under applicable state");
            content.AppendLine("and federal laws.");
            content.AppendLine();
            content.AppendLine("The incident occurred as described, and I have provided all relevant");
            content.AppendLine("documentation and evidence in support of this claim.");
            content.AppendLine();
            
            content.AppendLine("Signature: John Michael Davis");
            content.AppendLine("Date: January 20, 2024");
            content.AppendLine();
            content.AppendLine("================================================================================");
            content.AppendLine("This is a sample insurance claim document for testing NLP integration.");
            content.AppendLine("Generated: " + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"));
            content.AppendLine("================================================================================");
            
            File.WriteAllText(filePath, content.ToString(), Encoding.UTF8);
        }
    }
}
