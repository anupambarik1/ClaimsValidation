using Claims.Domain.DTOs;
using Claims.Domain.Entities;

namespace Claims.Services.Interfaces;

public interface IClaimsService
{
    Task<ClaimResponseDto> SubmitClaimAsync(ClaimSubmissionDto submission);
    Task<ClaimStatusDto?> GetClaimStatusAsync(Guid claimId);
    Task<List<Claim>> GetClaimsForUserAsync(string claimantId);
    Task<bool> UpdateClaimStatusAsync(Guid claimId, string status, string? specialistId = null);
}
