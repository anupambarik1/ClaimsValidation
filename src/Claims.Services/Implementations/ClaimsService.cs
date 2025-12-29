using Claims.Domain.DTOs;
using Claims.Domain.Entities;
using Claims.Domain.Enums;
using Claims.Infrastructure.Data;
using Claims.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Claims.Services.Implementations;

public class ClaimsService : IClaimsService
{
    private readonly ClaimsDbContext _context;
    private readonly IOcrService _ocrService;
    private readonly IRulesEngineService _rulesEngineService;
    private readonly IMlScoringService _mlScoringService;
    private readonly INotificationService _notificationService;

    public ClaimsService(
        ClaimsDbContext context,
        IOcrService ocrService,
        IRulesEngineService rulesEngineService,
        IMlScoringService mlScoringService,
        INotificationService notificationService)
    {
        _context = context;
        _ocrService = ocrService;
        _rulesEngineService = rulesEngineService;
        _mlScoringService = mlScoringService;
        _notificationService = notificationService;
    }

    public async Task<ClaimResponseDto> SubmitClaimAsync(ClaimSubmissionDto submission)
    {
        var claim = new Claim
        {
            ClaimId = Guid.NewGuid(),
            PolicyId = submission.PolicyId,
            ClaimantId = submission.ClaimantId,
            TotalAmount = submission.TotalAmount,
            Status = ClaimStatus.Submitted,
            SubmittedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow
        };

        _context.Claims.Add(claim);
        await _context.SaveChangesAsync();

        // Send notification
        await _notificationService.SendClaimStatusNotificationAsync(
            claim.ClaimId,
            submission.ClaimantId,
            NotificationType.ClaimReceived);

        return new ClaimResponseDto
        {
            ClaimId = claim.ClaimId,
            Status = claim.Status,
            SubmittedDate = claim.SubmittedDate,
            Message = "Claim submitted successfully"
        };
    }

    public async Task<ClaimStatusDto?> GetClaimStatusAsync(Guid claimId)
    {
        var claim = await _context.Claims
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);

        if (claim == null)
            return null;

        return new ClaimStatusDto
        {
            ClaimId = claim.ClaimId,
            Status = claim.Status,
            SubmittedDate = claim.SubmittedDate,
            LastUpdatedDate = claim.LastUpdatedDate,
            FraudScore = claim.FraudScore,
            ApprovalScore = claim.ApprovalScore,
            AssignedSpecialistId = claim.AssignedSpecialistId
        };
    }

    public async Task<List<Claim>> GetClaimsForUserAsync(string claimantId)
    {
        return await _context.Claims
            .Where(c => c.ClaimantId == claimantId)
            .OrderByDescending(c => c.SubmittedDate)
            .ToListAsync();
    }

    public async Task<bool> UpdateClaimStatusAsync(Guid claimId, string status, string? specialistId = null)
    {
        var claim = await _context.Claims.FirstOrDefaultAsync(c => c.ClaimId == claimId);
        
        if (claim == null)
            return false;

        if (Enum.TryParse<ClaimStatus>(status, out var claimStatus))
        {
            claim.Status = claimStatus;
            claim.LastUpdatedDate = DateTime.UtcNow;
            
            if (!string.IsNullOrEmpty(specialistId))
                claim.AssignedSpecialistId = specialistId;

            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }
}
