using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Claims.Services.Azure;

/// <summary>
/// Azure OpenAI service for NLP tasks like summarization, entity extraction, and fraud analysis
/// </summary>
public class AzureOpenAIService
{
    private readonly OpenAIClient? _client;
    private readonly ILogger<AzureOpenAIService> _logger;
    private readonly bool _isEnabled;
    private readonly string _deploymentName;

    public AzureOpenAIService(
        IConfiguration configuration,
        ILogger<AzureOpenAIService> logger)
    {
        _logger = logger;

        var endpoint = configuration["Azure:OpenAI:Endpoint"];
        var apiKey = configuration["Azure:OpenAI:ApiKey"];
        _isEnabled = configuration.GetValue<bool>("Azure:OpenAI:Enabled");
        _deploymentName = configuration["Azure:OpenAI:DeploymentName"] ?? "gpt-4";

        if (_isEnabled && !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
        {
            _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            _logger.LogInformation("Azure OpenAI initialized with deployment: {Deployment}", _deploymentName);
        }
        else
        {
            _logger.LogWarning("Azure OpenAI is disabled or not configured");
        }
    }

    public bool IsEnabled => _isEnabled && _client != null;

    /// <summary>
    /// Summarize a claim description using GPT-4
    /// </summary>
    public async Task<string> SummarizeClaimAsync(string claimDescription, string documentText)
    {
        if (!IsEnabled)
        {
            _logger.LogWarning("Azure OpenAI not enabled, skipping summarization");
            return claimDescription;
        }

        try
        {
            var prompt = $@"You are an insurance claims analyst. Summarize the following claim in 2-3 sentences, 
highlighting the key facts (what happened, when, estimated amount, and any red flags).

Claim Description:
{claimDescription}

Supporting Document Text:
{documentText}

Provide a concise summary:";

            var options = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage("You are an expert insurance claims analyst."),
                    new ChatRequestUserMessage(prompt)
                },
                MaxTokens = 200,
                Temperature = 0.3f
            };

            var response = await _client!.GetChatCompletionsAsync(options);
            var summary = response.Value.Choices[0].Message.Content;

            _logger.LogInformation("Claim summarized successfully");
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing claim with Azure OpenAI");
            return claimDescription;
        }
    }

    /// <summary>
    /// Extract entities from claim text (names, dates, amounts, locations)
    /// </summary>
    public async Task<ClaimEntities> ExtractEntitiesAsync(string text)
    {
        var entities = new ClaimEntities();

        if (!IsEnabled)
        {
            _logger.LogWarning("Azure OpenAI not enabled, skipping entity extraction");
            return entities;
        }

        try
        {
            var prompt = $@"Extract the following entities from this insurance claim text. 
Return as JSON with these fields: names (array), dates (array), amounts (array of numbers), 
locations (array), claimType (string: medical/auto/property/other).

Text:
{text}

Return only valid JSON:";

            var options = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage("You are an entity extraction assistant. Return only valid JSON."),
                    new ChatRequestUserMessage(prompt)
                },
                MaxTokens = 500,
                Temperature = 0.1f
            };

            var response = await _client!.GetChatCompletionsAsync(options);
            var jsonResponse = response.Value.Choices[0].Message.Content;

            // Parse JSON response (simplified - in production use System.Text.Json)
            // For now, just log the response
            _logger.LogInformation("Entities extracted: {Entities}", jsonResponse);
            entities.RawJson = jsonResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting entities with Azure OpenAI");
        }

        return entities;
    }

    /// <summary>
    /// Analyze claim narrative for fraud indicators
    /// </summary>
    public async Task<FraudNarrativeAnalysis> AnalyzeFraudNarrativeAsync(string claimDescription)
    {
        var analysis = new FraudNarrativeAnalysis();

        if (!IsEnabled)
        {
            _logger.LogWarning("Azure OpenAI not enabled, skipping fraud narrative analysis");
            return analysis;
        }

        try
        {
            var prompt = $@"Analyze this insurance claim description for potential fraud indicators. 
Look for: inconsistencies, vague details, unusual circumstances, known fraud patterns.

Claim Description:
{claimDescription}

Return a JSON response with:
- riskScore (0.0 to 1.0)
- riskLevel (Low/Medium/High)
- indicators (array of strings describing concerns)
- recommendation (Approve/Review/Investigate)

Return only valid JSON:";

            var options = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage("You are a fraud detection specialist. Be thorough but fair."),
                    new ChatRequestUserMessage(prompt)
                },
                MaxTokens = 500,
                Temperature = 0.2f
            };

            var response = await _client!.GetChatCompletionsAsync(options);
            var jsonResponse = response.Value.Choices[0].Message.Content;

            _logger.LogInformation("Fraud narrative analysis completed");
            analysis.RawJson = jsonResponse;
            analysis.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing fraud narrative with Azure OpenAI");
            analysis.ErrorMessage = ex.Message;
        }

        return analysis;
    }

    /// <summary>
    /// Generate automated response for claim decision
    /// </summary>
    public async Task<string> GenerateClaimResponseAsync(
        string claimantName,
        string decision,
        string reason,
        decimal? amount)
    {
        if (!IsEnabled)
        {
            return $"Dear {claimantName}, your claim has been {decision.ToLower()}. {reason}";
        }

        try
        {
            var prompt = $@"Write a professional and empathetic insurance claim notification email.

Details:
- Claimant Name: {claimantName}
- Decision: {decision}
- Reason: {reason}
- Amount (if approved): {(amount.HasValue ? amount.Value.ToString("C") : "N/A")}

Write a brief, professional email (3-4 sentences). Be empathetic if rejected.";

            var options = new ChatCompletionsOptions
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage("You write professional insurance correspondence."),
                    new ChatRequestUserMessage(prompt)
                },
                MaxTokens = 300,
                Temperature = 0.5f
            };

            var response = await _client!.GetChatCompletionsAsync(options);
            return response.Value.Choices[0].Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating claim response");
            return $"Dear {claimantName}, your claim has been {decision.ToLower()}. {reason}";
        }
    }
}

public class ClaimEntities
{
    public List<string> Names { get; set; } = new();
    public List<string> Dates { get; set; } = new();
    public List<decimal> Amounts { get; set; } = new();
    public List<string> Locations { get; set; } = new();
    public string? ClaimType { get; set; }
    public string? RawJson { get; set; }
}

public class FraudNarrativeAnalysis
{
    public bool Success { get; set; }
    public decimal RiskScore { get; set; }
    public string RiskLevel { get; set; } = "Unknown";
    public List<string> Indicators { get; set; } = new();
    public string? Recommendation { get; set; }
    public string? RawJson { get; set; }
    public string? ErrorMessage { get; set; }
}
