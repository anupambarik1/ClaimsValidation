using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Claims.Services.Azure;

/// <summary>
/// Azure Communication Services for email notifications
/// </summary>
public class AzureEmailService
{
    private readonly EmailClient? _emailClient;
    private readonly ILogger<AzureEmailService> _logger;
    private readonly bool _isEnabled;
    private readonly string _senderEmail;

    public AzureEmailService(
        IConfiguration configuration,
        ILogger<AzureEmailService> logger)
    {
        _logger = logger;

        var connectionString = configuration["Azure:CommunicationServices:ConnectionString"];
        _isEnabled = configuration.GetValue<bool>("Azure:CommunicationServices:Enabled");
        _senderEmail = configuration["Azure:CommunicationServices:SenderEmail"] ?? "DoNotReply@claims.azurecomm.net";

        if (_isEnabled && !string.IsNullOrEmpty(connectionString))
        {
            _emailClient = new EmailClient(connectionString);
            _logger.LogInformation("Azure Communication Services Email initialized");
        }
        else
        {
            _logger.LogWarning("Azure Communication Services Email is disabled or not configured");
        }
    }

    public bool IsEnabled => _isEnabled && _emailClient != null;

    /// <summary>
    /// Send email notification
    /// </summary>
    public async Task<bool> SendEmailAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        string? plainTextBody = null)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("Azure Email not enabled. Would send to: {Email}, Subject: {Subject}", 
                recipientEmail, subject);
            return false;
        }

        try
        {
            var emailMessage = new EmailMessage(
                senderAddress: _senderEmail,
                recipientAddress: recipientEmail,
                content: new EmailContent(subject)
                {
                    Html = htmlBody,
                    PlainText = plainTextBody ?? StripHtml(htmlBody)
                });

            var operation = await _emailClient!.SendAsync(WaitUntil.Completed, emailMessage);

            if (operation.HasCompleted)
            {
                _logger.LogInformation("Email sent to {Recipient}: {Subject}", recipientEmail, subject);
                return true;
            }

            _logger.LogWarning("Email operation did not complete for {Recipient}", recipientEmail);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Recipient}", recipientEmail);
            return false;
        }
    }

    /// <summary>
    /// Send claim status notification
    /// </summary>
    public async Task<bool> SendClaimNotificationAsync(
        string recipientEmail,
        string recipientName,
        Guid claimId,
        string status,
        string? message = null)
    {
        var subject = $"Claim {claimId.ToString()[..8].ToUpper()} - Status Update: {status}";
        
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #0078D4; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .status {{ font-size: 24px; font-weight: bold; color: #0078D4; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Claims Validation System</h1>
        </div>
        <div class='content'>
            <p>Dear {recipientName},</p>
            <p>Your claim status has been updated.</p>
            <p><strong>Claim ID:</strong> {claimId}</p>
            <p><strong>New Status:</strong> <span class='status'>{status}</span></p>
            {(string.IsNullOrEmpty(message) ? "" : $"<p><strong>Details:</strong> {message}</p>")}
            <p>If you have any questions, please contact our support team.</p>
            <p>Best regards,<br/>Claims Validation Team</p>
        </div>
        <div class='footer'>
            <p>This is an automated message. Please do not reply directly to this email.</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(recipientEmail, subject, htmlBody);
    }

    /// <summary>
    /// Send claim decision notification
    /// </summary>
    public async Task<bool> SendDecisionNotificationAsync(
        string recipientEmail,
        string recipientName,
        Guid claimId,
        string decision,
        string reason,
        decimal? approvedAmount = null)
    {
        var statusColor = decision.ToLower() switch
        {
            "approved" or "autoapprove" => "#28a745",
            "rejected" or "reject" => "#dc3545",
            _ => "#ffc107"
        };

        var subject = $"Claim Decision: {decision} - Claim {claimId.ToString()[..8].ToUpper()}";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #0078D4; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .decision {{ font-size: 24px; font-weight: bold; color: {statusColor}; padding: 10px; background: white; border-left: 4px solid {statusColor}; }}
        .amount {{ font-size: 20px; color: #28a745; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Claims Validation System</h1>
            <p>Claim Decision Notification</p>
        </div>
        <div class='content'>
            <p>Dear {recipientName},</p>
            <p>A decision has been made on your insurance claim.</p>
            
            <p><strong>Claim ID:</strong> {claimId}</p>
            <div class='decision'>{decision.ToUpper()}</div>
            
            <p><strong>Reason:</strong> {reason}</p>
            
            {(approvedAmount.HasValue ? $"<p class='amount'><strong>Approved Amount:</strong> {approvedAmount:C}</p>" : "")}
            
            <p>If you have any questions about this decision, please contact our claims department.</p>
            
            <p>Best regards,<br/>Claims Validation Team</p>
        </div>
        <div class='footer'>
            <p>This is an automated message from our AI-powered claims processing system.</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(recipientEmail, subject, htmlBody);
    }

    private static string StripHtml(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", " ")
            .Replace("&nbsp;", " ")
            .Replace("  ", " ")
            .Trim();
    }
}
