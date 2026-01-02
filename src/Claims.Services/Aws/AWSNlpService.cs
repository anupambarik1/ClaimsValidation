using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Claims.Services.Interfaces;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Amazon.Runtime;
using Amazon;
using System.Text.Json;

namespace Claims.Services.Aws;

/// <summary>
/// AWS NLP service combining Bedrock (Claude 3) and Comprehend for advanced claim analysis.
/// - Bedrock: Summarization, fraud analysis, entity extraction, response generation
/// - Comprehend: Sentiment analysis, entity detection
/// </summary>
public class AWSNlpService : INlpService
{
    private readonly ILogger<AWSNlpService> _logger;
    private readonly bool _isEnabled;
    private readonly AmazonComprehendClient _comprehendClient;
    private readonly AWSBedrockService _bedrockService;

    public AWSNlpService(IConfiguration configuration, ILogger<AWSNlpService> logger, AWSBedrockService bedrockService)
    {
        _logger = logger;
        _isEnabled = configuration.GetValue<bool>("AWS:Enabled", false);
        
        var accessKey = configuration["AWS:AccessKey"];
        var secretKey = configuration["AWS:SecretKey"];
        var regionName = configuration["AWS:Region"] ?? "us-east-1";
        
        AWSCredentials credentials = new BasicAWSCredentials(accessKey, secretKey);
        var region = RegionEndpoint.GetBySystemName(regionName);
        
        _comprehendClient = new AmazonComprehendClient(credentials, region);
        _bedrockService = bedrockService;

        if (!_isEnabled)
        {
            _logger.LogWarning("AWS NLP service is disabled");
        }
    }

    public bool IsEnabled => _isEnabled;

    public async Task<string> SummarizeClaimAsync(string claimDescription, string documentText)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("AWS NLP not enabled, returning original");
            return claimDescription;
        }

        try
        {
            var combined = $"{claimDescription}\n\nDocument:\n{documentText}";
            var truncated = combined.Substring(0, Math.Min(3000, combined.Length));

            var prompt = $@"Summarize this insurance claim in 2-3 sentences. Focus on what, when, where, and amount.

{truncated}

Summary:";

            var summary = await _bedrockService.InvokeClaudeAsync(prompt);
            _logger.LogInformation("Claim summarized successfully");
            return summary.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing with Bedrock");
            return claimDescription;
        }
    }

    public async Task<string> AnalyzeFraudNarrativeAsync(string claimDescription)
    {
        if (!IsEnabled)
        {
            return JsonSerializer.Serialize(new
            {
                riskScore = 0.0m,
                riskLevel = "Low",
                indicators = new List<string>(),
                recommendation = "Approve"
            });
        }

        try
        {
            var truncated = claimDescription.Substring(0, Math.Min(2000, claimDescription.Length));

            var sentiment = await _comprehendClient.DetectSentimentAsync(new DetectSentimentRequest
            {
                Text = truncated,
                LanguageCode = "en"
            });

            var fraudPrompt = $@"Analyze this claim for fraud risk (0.0 to 1.0). Look for inconsistencies, vague details, unrealistic claims.

{truncated}

Return ONLY JSON:
{{
  ""riskScore"": <0.0-1.0>,
  ""riskLevel"": ""Low""|""Medium""|""High"",
  ""indicators"": [""indicator1""],
  ""recommendation"": ""Approve""|""Review""|""Investigate""
}}";

            var fraudAnalysis = await _bedrockService.InvokeClaudeAsync(fraudPrompt);
            
            try
            {
                var parsed = JsonSerializer.Deserialize<JsonElement>(fraudAnalysis);
                var riskScore = parsed.GetProperty("riskScore").GetDouble();
                var sentimentBonus = sentiment.Sentiment == SentimentType.NEGATIVE ? 0.15 : 0.0;
                var adjustedScore = Math.Min(1.0, riskScore + sentimentBonus);

                var result = new
                {
                    riskScore = adjustedScore,
                    riskLevel = parsed.GetProperty("riskLevel").GetString(),
                    indicators = parsed.GetProperty("indicators"),
                    recommendation = parsed.GetProperty("recommendation").GetString(),
                    sentimentAnalysis = sentiment.Sentiment.Value
                };

                _logger.LogInformation("Fraud analysis completed. Risk: {RiskLevel}", result.riskLevel);
                return JsonSerializer.Serialize(result);
            }
            catch
            {
                return fraudAnalysis;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing fraud");
            return JsonSerializer.Serialize(new
            {
                riskScore = 0.5m,
                riskLevel = "Medium",
                indicators = new[] { "Analysis failed" },
                recommendation = "Review"
            });
        }
    }

    public async Task<string> ExtractEntitiesAsync(string text)
    {
        if (!IsEnabled)
        {
            return JsonSerializer.Serialize(new
            {
                names = new List<string>(),
                dates = new List<string>(),
                amounts = new List<decimal>(),
                locations = new List<string>(),
                claimType = "unknown"
            });
        }

        try
        {
            var truncated = text.Substring(0, Math.Min(500, text.Length));

            var entities = await _comprehendClient.DetectEntitiesAsync(new DetectEntitiesRequest
            {
                Text = truncated,
                LanguageCode = "en"
            });

            var names = entities.Entities.Where(e => e.Type == "PERSON").Select(e => e.Text).ToList();
            var dates = entities.Entities.Where(e => e.Type == "DATE").Select(e => e.Text).ToList();
            var locations = entities.Entities.Where(e => e.Type == "LOCATION").Select(e => e.Text).ToList();

            var classPrompt = $@"Extract claim type and amounts from:
{truncated}

Return JSON:
{{
  ""claimType"": ""medical""|""auto""|""property""|""life""|""other"",
  ""amounts"": [100.00, 200.00]
}}";

            var classification = await _bedrockService.InvokeClaudeAsync(classPrompt);
            
            try
            {
                var parsed = JsonSerializer.Deserialize<JsonElement>(classification);
                var amounts = parsed.GetProperty("amounts")
                    .EnumerateArray()
                    .Select(a => Convert.ToDecimal(a.GetDouble()))
                    .ToList();
                var claimType = parsed.GetProperty("claimType").GetString();

                var result = new
                {
                    names = names,
                    dates = dates,
                    amounts = amounts,
                    locations = locations,
                    claimType = claimType
                };

                _logger.LogInformation("Entities extracted: {Count} names, {DateCount} dates", 
                    names.Count, dates.Count);
                
                return JsonSerializer.Serialize(result);
            }
            catch
            {
                return JsonSerializer.Serialize(new
                {
                    names = names,
                    dates = dates,
                    amounts = new List<decimal>(),
                    locations = locations,
                    claimType = "unknown"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting entities");
            return JsonSerializer.Serialize(new
            {
                names = new List<string>(),
                dates = new List<string>(),
                amounts = new List<decimal>(),
                locations = new List<string>(),
                claimType = "unknown"
            });
        }
    }

    public async Task<string> GenerateClaimResponseAsync(string claimantName, string decision, string reason, decimal? amount)
    {
        if (!IsEnabled)
        {
            return GenerateTemplate(claimantName, decision, reason, amount);
        }

        try
        {
            var prompt = $@"Write a professional insurance claim decision letter.

Claimant: {claimantName}
Decision: {decision}
Reason: {reason}
Amount: {(amount.HasValue ? $"${amount.Value:F2}" : "N/A")}

Letter:";

            var response = await _bedrockService.InvokeClaudeAsync(prompt);
            _logger.LogInformation("Response generated for {ClaimantName}", claimantName);
            return response.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response");
            return GenerateTemplate(claimantName, decision, reason, amount);
        }
    }

    private static string GenerateTemplate(string claimantName, string decision, string reason, decimal? amount)
    {
        return $@"Dear {claimantName},

Your claim has been {decision.ToLower()}. {reason}

{(amount.HasValue ? $"Amount: ${amount.Value:F2}" : "")}

Best regards,
Claims Team";
    }
}
