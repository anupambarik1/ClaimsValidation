using Claims.Domain.Enums;

namespace Claims.Domain.Entities;

public class Claim
{
    public Guid ClaimId { get; set; }
    public string PolicyId { get; set; } = string.Empty;
    public string ClaimantId { get; set; } = string.Empty;
    public ClaimStatus Status { get; set; }
    public DateTime SubmittedDate { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal? FraudScore { get; set; }
    public decimal? ApprovalScore { get; set; }
    public string? AssignedSpecialistId { get; set; }
    
    // Navigation properties
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    public virtual ICollection<Decision> Decisions { get; set; } = new List<Decision>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
