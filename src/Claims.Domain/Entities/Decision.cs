using Claims.Domain.Enums;

namespace Claims.Domain.Entities;

public class Decision
{
    public Guid DecisionId { get; set; }
    public Guid ClaimId { get; set; }
    public DecisionStatus Status { get; set; }
    public DateTime DecisionDate { get; set; }
    public string? Reason { get; set; }
    public string? ReviewerId { get; set; } // "AI_SYSTEM" for auto-decisions or SpecialistId
    public string? DecidedBy { get; set; } // "System" or SpecialistId
    public decimal? FraudScore { get; set; }
    public decimal? ApprovalScore { get; set; }
    
    // Navigation property
    public virtual Claim Claim { get; set; } = null!;
}
