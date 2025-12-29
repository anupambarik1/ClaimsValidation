using Claims.Domain.Entities;

namespace Claims.Services.Interfaces;

public interface IMlScoringService
{
    Task<(decimal FraudScore, decimal ApprovalScore)> ScoreClaimAsync(Claim claim);
    Task<string> DetermineDecisionAsync(decimal fraudScore, decimal approvalScore);
}
