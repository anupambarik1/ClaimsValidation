using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Claims.Services.Interfaces;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace Claims.Services.Aws;

/// <summary>
/// AWS SES email service implementation using AWSSDK.SimpleEmail.
/// Uses default AWS credentials.
/// </summary>
public class AWSEmailService : IEmailService
{
    private readonly ILogger<AWSEmailService> _logger;
    private readonly bool _isEnabled;
    private readonly string? _fromEmail;
    private readonly AmazonSimpleEmailServiceClient _sesClient;

    public AWSEmailService(IConfiguration configuration, ILogger<AWSEmailService> logger)
    {
        _logger = logger;
        _isEnabled = configuration.GetValue<bool>("AWS:Enabled", false);
        _fromEmail = configuration["AWS:SES:FromEmail"] ?? configuration["SmtpSettings:SenderEmail"];
        if (!_isEnabled) _logger.LogWarning("AWS Email (SES) is disabled in configuration.");

        _sesClient = new AmazonSimpleEmailServiceClient();
    }

    public bool IsEnabled => _isEnabled && !string.IsNullOrEmpty(_fromEmail);

    public async Task<bool> SendEmailAsync(string recipientEmail, string subject, string htmlBody, string? plainTextBody = null)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("SES not enabled or FromEmail missing. Would send to {Recipient}", recipientEmail);
            return false;
        }

        try
        {
            var destination = new Destination { ToAddresses = new List<string> { recipientEmail } };
            var content = new Message
            {
                Subject = new Content(subject),
                Body = new Body
                {
                    Html = new Content(htmlBody),
                    Text = new Content(plainTextBody ?? System.Text.RegularExpressions.Regex.Replace(htmlBody, "<[^>]*>", " "))
                }
            };

            var request = new SendEmailRequest
            {
                Source = _fromEmail,
                Destination = destination,
                Message = content
            };

            var resp = await _sesClient.SendEmailAsync(request);
            return resp.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email via SES");
            return false;
        }
    }

    public Task<bool> SendClaimNotificationAsync(string recipientEmail, string recipientName, Guid claimId, string status, string? message = null)
    {
        var subject = $"Claim {claimId.ToString()[..8].ToUpper()} - Status Update: {status}";
        var body = $"Dear {recipientName},\nYour claim {claimId} status updated to {status}. {message ?? string.Empty}";
        return SendEmailAsync(recipientEmail, subject, body, body);
    }

    public Task<bool> SendDecisionNotificationAsync(string recipientEmail, string recipientName, Guid claimId, string decision, string reason, decimal? approvedAmount = null)
    {
        var subject = $"Claim Decision: {decision} - Claim {claimId.ToString()[..8].ToUpper()}";
        var body = $"Dear {recipientName},\nYour claim decision: {decision}. Reason: {reason}. Approved Amount: {(approvedAmount.HasValue ? approvedAmount.Value.ToString("C") : "N/A")}";
        return SendEmailAsync(recipientEmail, subject, body, body);
    }
}
