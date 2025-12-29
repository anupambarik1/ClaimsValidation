using Claims.Domain.Entities;
using Claims.Infrastructure.Data;
using Claims.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Claims.Services.Implementations;

/// <summary>
/// Business rules engine for claims validation
/// Implements configurable policy rules without requiring external licensed products
/// </summary>
public class RulesEngineService : IRulesEngineService
{
    private readonly ClaimsDbContext _context;
    private readonly IConfiguration _configuration;
    
    // Configurable thresholds
    private readonly decimal _maxClaimAmount;
    private readonly int _maxClaimsPerMonth;
    private readonly int _minDaysBetweenClaims;

    public RulesEngineService(ClaimsDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        
        // Load configurable thresholds from settings
        _maxClaimAmount = configuration.GetValue<decimal>("RulesEngine:MaxClaimAmount", 50000m);
        _maxClaimsPerMonth = configuration.GetValue<int>("RulesEngine:MaxClaimsPerMonth", 3);
        _minDaysBetweenClaims = configuration.GetValue<int>("RulesEngine:MinDaysBetweenClaims", 7);
    }

    public async Task<(bool IsValid, string? Reason)> ValidatePolicyAsync(Claim claim)
    {
        var validationResults = new List<(string Rule, bool Passed, string? Message)>();

        // Rule 1: Check claim amount limits
        var amountCheck = await ValidateClaimAmountAsync(claim);
        validationResults.Add(("ClaimAmountLimit", amountCheck.passed, amountCheck.message));

        // Rule 2: Validate policy ID format and existence
        var policyCheck = ValidatePolicyId(claim.PolicyId);
        validationResults.Add(("PolicyIdValid", policyCheck.passed, policyCheck.message));

        // Rule 3: Check for duplicate claims (same amount within short period)
        var duplicateCheck = await CheckDuplicateClaimsAsync(claim);
        validationResults.Add(("NoDuplicateClaims", duplicateCheck.passed, duplicateCheck.message));

        // Rule 4: Verify claimant exists and is valid
        var claimantCheck = ValidateClaimantId(claim.ClaimantId);
        validationResults.Add(("ClaimantValid", claimantCheck.passed, claimantCheck.message));

        // Rule 5: Check claim frequency (max claims per month)
        var frequencyCheck = await CheckClaimFrequencyAsync(claim);
        validationResults.Add(("ClaimFrequency", frequencyCheck.passed, frequencyCheck.message));

        // Rule 6: Validate required documents exist
        var documentCheck = ValidateDocuments(claim);
        validationResults.Add(("RequiredDocuments", documentCheck.passed, documentCheck.message));

        // Aggregate results
        var failedRules = validationResults.Where(r => !r.Passed).ToList();
        
        if (failedRules.Any())
        {
            var reasons = string.Join("; ", failedRules.Select(r => r.Message));
            return (false, reasons);
        }

        return (true, null);
    }

    private async Task<(bool passed, string? message)> ValidateClaimAmountAsync(Claim claim)
    {
        if (claim.TotalAmount <= 0)
            return (false, "Claim amount must be greater than zero");

        if (claim.TotalAmount > _maxClaimAmount)
            return (false, $"Claim amount ${claim.TotalAmount:N2} exceeds maximum limit of ${_maxClaimAmount:N2}");

        // Check coverage based on policy
        var hasCoverage = await CheckCoverageAsync(claim.PolicyId, claim.TotalAmount);
        if (!hasCoverage)
            return (false, $"Insufficient coverage for claim amount ${claim.TotalAmount:N2}");

        return (true, null);
    }

    private (bool passed, string? message) ValidatePolicyId(string policyId)
    {
        if (string.IsNullOrWhiteSpace(policyId))
            return (false, "Policy ID is required");

        // Policy ID format: POL-XXXXX-XXXX (alphanumeric)
        if (policyId.Length < 5)
            return (false, "Invalid policy ID format");

        // In production, this would check against a policy database
        // For demo, we accept any non-empty policy ID
        return (true, null);
    }

    private async Task<(bool passed, string? message)> CheckDuplicateClaimsAsync(Claim claim)
    {
        var duplicateWindow = DateTime.UtcNow.AddDays(-7);
        
        var duplicateExists = await _context.Claims
            .Where(c => c.ClaimId != claim.ClaimId)
            .Where(c => c.ClaimantId == claim.ClaimantId)
            .Where(c => c.TotalAmount == claim.TotalAmount)
            .Where(c => c.SubmittedDate > duplicateWindow)
            .AnyAsync();

        if (duplicateExists)
            return (false, "Duplicate claim detected: Same amount submitted within 7 days");

        return (true, null);
    }

    private (bool passed, string? message) ValidateClaimantId(string claimantId)
    {
        if (string.IsNullOrWhiteSpace(claimantId))
            return (false, "Claimant ID is required");

        // Email format validation if claimantId is email
        if (claimantId.Contains("@") && !IsValidEmail(claimantId))
            return (false, "Invalid claimant email format");

        return (true, null);
    }

    private async Task<(bool passed, string? message)> CheckClaimFrequencyAsync(Claim claim)
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        
        var claimsThisMonth = await _context.Claims
            .Where(c => c.ClaimId != claim.ClaimId)
            .Where(c => c.ClaimantId == claim.ClaimantId)
            .Where(c => c.SubmittedDate >= monthStart)
            .CountAsync();

        if (claimsThisMonth >= _maxClaimsPerMonth)
            return (false, $"Maximum claims per month ({_maxClaimsPerMonth}) exceeded");

        // Check minimum days between claims
        var lastClaim = await _context.Claims
            .Where(c => c.ClaimId != claim.ClaimId)
            .Where(c => c.ClaimantId == claim.ClaimantId)
            .OrderByDescending(c => c.SubmittedDate)
            .FirstOrDefaultAsync();

        if (lastClaim != null)
        {
            var daysSinceLastClaim = (DateTime.UtcNow - lastClaim.SubmittedDate).Days;
            if (daysSinceLastClaim < _minDaysBetweenClaims)
                return (false, $"Minimum {_minDaysBetweenClaims} days required between claims");
        }

        return (true, null);
    }

    private (bool passed, string? message) ValidateDocuments(Claim claim)
    {
        // For claims over certain amount, documents are required
        if (claim.TotalAmount > 1000 && (claim.Documents == null || !claim.Documents.Any()))
            return (false, "Supporting documents required for claims over $1,000");

        return (true, null);
    }

    public async Task<bool> CheckCoverageAsync(string policyId, decimal amount)
    {
        // In production, this would query an external policy management system
        // For demo purposes, we simulate coverage checks
        
        await Task.Delay(10); // Simulate external API call

        // Simulated coverage tiers based on policy prefix
        if (policyId.StartsWith("PREM", StringComparison.OrdinalIgnoreCase))
            return amount <= 100000m; // Premium policies: $100k coverage
        
        if (policyId.StartsWith("STD", StringComparison.OrdinalIgnoreCase))
            return amount <= 25000m; // Standard policies: $25k coverage
        
        if (policyId.StartsWith("BASIC", StringComparison.OrdinalIgnoreCase))
            return amount <= 10000m; // Basic policies: $10k coverage

        // Default coverage
        return amount <= _maxClaimAmount;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
