using Claims.Domain.Enums;

namespace Claims.Domain.DTOs;

public class ClaimStatusDto
{
    public Guid ClaimId { get; set; }
    public ClaimStatus Status { get; set; }
    public DateTime SubmittedDate { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public decimal? FraudScore { get; set; }
    public decimal? ApprovalScore { get; set; }
    public string? AssignedSpecialistId { get; set; }
}
