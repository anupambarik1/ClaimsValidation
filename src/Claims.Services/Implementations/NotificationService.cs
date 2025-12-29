using Claims.Domain.Entities;
using Claims.Domain.Enums;
using Claims.Infrastructure.Data;
using Claims.Services.Interfaces;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace Claims.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly ClaimsDbContext _context;
    private readonly IConfiguration _configuration;

    public NotificationService(ClaimsDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string recipientEmail, string subject, string body)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _configuration["SmtpSettings:SenderName"] ?? "Claims System",
                _configuration["SmtpSettings:SenderEmail"] ?? "noreply@claims.com"));
            message.To.Add(new MailboxAddress("", recipientEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            var host = _configuration["SmtpSettings:Host"];
            var portStr = _configuration["SmtpSettings:Port"];
            var username = _configuration["SmtpSettings:Username"];
            var password = _configuration["SmtpSettings:Password"];

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
            {
                // SMTP not configured, just log
                Console.WriteLine($"[Email Not Sent - SMTP Not Configured] To: {recipientEmail}, Subject: {subject}");
                return;
            }

            var port = int.TryParse(portStr, out var p) ? p : 587;

            await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            Console.WriteLine($"Email sent to {recipientEmail}: {subject}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email sending failed: {ex.Message}");
            // Don't throw - email failures shouldn't break the claim processing
        }
    }

    public async Task SendClaimStatusNotificationAsync(
        Guid claimId,
        string recipientEmail,
        NotificationType notificationType)
    {
        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            ClaimId = claimId,
            RecipientEmail = recipientEmail,
            NotificationType = notificationType,
            SentDate = DateTime.UtcNow,
            Status = NotificationStatus.Pending,
            MessageBody = GetNotificationMessage(notificationType)
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // TODO: Actually send the email via Azure Communication Services
        await SendEmailAsync(recipientEmail, GetSubject(notificationType), notification.MessageBody);

        // Update status
        notification.Status = NotificationStatus.Sent;
        await _context.SaveChangesAsync();
    }

    private string GetNotificationMessage(NotificationType type)
    {
        return type switch
        {
            NotificationType.ClaimReceived => "Your claim has been received and is being processed.",
            NotificationType.StatusUpdate => "Your claim status has been updated.",
            NotificationType.DecisionMade => "A decision has been made on your claim.",
            NotificationType.DocumentsRequested => "Additional documents are required for your claim.",
            NotificationType.ManualReviewAssigned => "Your claim has been assigned for manual review.",
            _ => "Claim notification"
        };
    }

    private string GetSubject(NotificationType type)
    {
        return type switch
        {
            NotificationType.ClaimReceived => "Claim Received",
            NotificationType.StatusUpdate => "Claim Status Update",
            NotificationType.DecisionMade => "Claim Decision",
            NotificationType.DocumentsRequested => "Documents Required",
            NotificationType.ManualReviewAssigned => "Manual Review Assigned",
            _ => "Claim Notification"
        };
    }
}
