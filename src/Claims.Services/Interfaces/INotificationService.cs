using Claims.Domain.Enums;

namespace Claims.Services.Interfaces;

public interface INotificationService
{
    Task SendEmailAsync(string recipientEmail, string subject, string body);
    Task SendClaimStatusNotificationAsync(Guid claimId, string recipientEmail, NotificationType notificationType);
}
