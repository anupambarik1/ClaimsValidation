using Claims.Domain.Enums;

namespace Claims.Domain.DTOs;

public class ClaimResponseDto
{
    public Guid ClaimId { get; set; }
    public ClaimStatus Status { get; set; }
    public DateTime SubmittedDate { get; set; }
    public string Message { get; set; } = string.Empty;
}
