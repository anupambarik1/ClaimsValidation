namespace Claims.Services.Interfaces;

public interface IEmailService
{
    bool IsEnabled { get; }
    Task<bool> SendEmailAsync(string recipientEmail, string subject, string htmlBody, string? plainTextBody = null);
    Task<bool> SendClaimNotificationAsync(string recipientEmail, string recipientName, Guid claimId, string status, string? message = null);
    Task<bool> SendDecisionNotificationAsync(string recipientEmail, string recipientName, Guid claimId, string decision, string reason, decimal? approvedAmount = null);
}
