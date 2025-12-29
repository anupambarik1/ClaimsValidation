namespace Claims.Domain.Enums;

public enum ClaimStatus
{
    Submitted,
    Processing,
    AutoApproved,
    Rejected,
    ManualReview,
    Approved,
    UnderReview,
    PendingDocuments
}
