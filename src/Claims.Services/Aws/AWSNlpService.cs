using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Claims.Services.Interfaces;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using System.Text.Json;

namespace Claims.Services.Aws;

/// <summary>
/// AWS Comprehend-based NLP service for entity extraction, key phrases, and sentiment analysis.
/// Falls back to simple heuristics when Comprehend not enabled.
/// </summary>
public class AWSNlpService : INlpService
{
    private readonly ILogger<AWSNlpService> _logger;
    private readonly bool _isEnabled;
    private readonly AmazonComprehendClient _comprehendClient;

    public AWSNlpService(IConfiguration configuration, ILogger<AWSNlpService> logger)
    {
        _logger = logger;
        _isEnabled = configuration.GetValue<bool>("AWS:Enabled", false);
        _comprehendClient = new AmazonComprehendClient();

        if (!_isEnabled)
        {
            _logger.LogWarning("AWS Comprehend is disabled in configuration");
        }
    }

    public bool IsEnabled => _isEnabled;

    public async Task<string> SummarizeClaimAsync(string claimDescription, string documentText)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("Comprehend not enabled, returning original description");
            return claimDescription;
        }

        try
        {
            // Use key phrase detection as a proxy for summarization
            var text = $"{claimDescription} {documentText}".Substring(0, Math.Min(400, (claimDescription + documentText).Length));
            var resp = await _comprehendClient.DetectKeyPhrasesAsync(new DetectKeyPhrasesRequest
            {
                Text = text,
                LanguageCode = "en"
            });

            var phrases = string.Join(", ", resp.KeyPhrases.OrderByDescending(p => p.Score).Take(5).Select(p => p.Text));
            return $"{claimDescription}. Key topics: {phrases}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing with Comprehend");
            return claimDescription;
        }
    }

    public async Task<string> AnalyzeFraudNarrativeAsync(string claimDescription)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("Comprehend not enabled for fraud analysis. Returning default.");
            return "{ \"riskScore\": 0.0, \"riskLevel\": \"Low\", \"indicators\": [] }";
        }

        try
        {
            // Use sentiment analysis as a heuristic for fraud (negative sentiment = higher risk)
            var text = claimDescription.Substring(0, Math.Min(500, claimDescription.Length));
            var resp = await _comprehendClient.DetectSentimentAsync(new DetectSentimentRequest
            {
                Text = text,
                LanguageCode = "en"
            });

            // Map sentiment to fraud risk score
            var riskScore = resp.Sentiment == SentimentType.NEGATIVE ? 0.6m : 
                           resp.Sentiment == SentimentType.MIXED ? 0.4m : 0.2m;
            var riskLevel = riskScore > 0.7m ? "High" : riskScore > 0.4m ? "Medium" : "Low";

            var result = new
            {
                riskScore = riskScore,
                riskLevel = riskLevel,
                indicators = new[] { $"Sentiment: {resp.Sentiment}" },
                recommendation = riskScore > 0.7m ? "Investigate" : "Approve"
            };
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing fraud with Comprehend");
            return "{ \"riskScore\": 0.5, \"riskLevel\": \"Medium\", \"indicators\": [\"Analysis failed\"] }";
        }
    }

    public async Task<string> ExtractEntitiesAsync(string text)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("Comprehend not enabled for entity extraction");
            return "{ \"names\": [], \"dates\": [], \"amounts\": [], \"locations\": [], \"claimType\": \"other\" }";
        }

        try
        {
            var textToAnalyze = text.Substring(0, Math.Min(500, text.Length));
            var resp = await _comprehendClient.DetectEntitiesAsync(new DetectEntitiesRequest
            {
                Text = textToAnalyze,
                LanguageCode = "en"
            });

            var names = resp.Entities.Where(e => e.Type == "PERSON").Select(e => e.Text).ToList();
            var dates = resp.Entities.Where(e => e.Type == "DATE").Select(e => e.Text).ToList();
            var locations = resp.Entities.Where(e => e.Type == "LOCATION").Select(e => e.Text).ToList();
            var amounts = new List<string>(); // Comprehend doesn't extract amounts, would need custom parsing

            var result = new
            {
                names = names,
                dates = dates,
                amounts = amounts,
                locations = locations,
                claimType = "other"
            };
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting entities with Comprehend");
            return "{ \"names\": [], \"dates\": [], \"amounts\": [], \"locations\": [], \"claimType\": \"other\" }";
        }
    }

    public Task<string> GenerateClaimResponseAsync(string claimantName, string decision, string reason, decimal? amount)
    {
        // Comprehend is for analysis only; use templated response for generation
        var response = $"Dear {claimantName},\n\nYour claim has been {decision.ToLower()}.\n\nReason: {reason}\n\n" +
                      (amount.HasValue ? $"Approved Amount: {amount.Value:C}\n\n" : "") +
                      "If you have questions, please contact our support team.\n\nBest regards,\nClaims Team";
        return Task.FromResult(response);
    }
}
