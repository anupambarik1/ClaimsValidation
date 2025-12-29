using Claims.Domain.Enums;

namespace Claims.Domain.Entities;

public class Notification
{
    public Guid NotificationId { get; set; }
    public Guid ClaimId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public NotificationType NotificationType { get; set; }
    public DateTime SentDate { get; set; }
    public NotificationStatus Status { get; set; }
    public string? MessageBody { get; set; }
    
    // Navigation property
    public virtual Claim Claim { get; set; } = null!;
}
