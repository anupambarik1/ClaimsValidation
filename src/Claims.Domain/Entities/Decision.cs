using Claims.Domain.Enums;

namespace Claims.Domain.Entities;

public class Decision
{
    public Guid DecisionId { get; set; }
    public Guid ClaimId { get; set; }
    public DecisionStatus DecisionStatus { get; set; }
    public DateTime DecisionDate { get; set; }
    public string? Reason { get; set; }
    public string? DecidedBy { get; set; } // "System" or SpecialistId
    public decimal? FraudScore { get; set; }
    public decimal? ApprovalScore { get; set; }
    
    // Navigation property
    public virtual Claim Claim { get; set; } = null!;
}
