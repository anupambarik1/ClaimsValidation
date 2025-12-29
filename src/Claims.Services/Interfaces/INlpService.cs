namespace Claims.Services.Interfaces;

public interface INlpService
{
    bool IsEnabled { get; }
    Task<string> SummarizeClaimAsync(string claimDescription, string documentText);
    Task<string> AnalyzeFraudNarrativeAsync(string claimDescription);
    Task<string> ExtractEntitiesAsync(string text);
    Task<string> GenerateClaimResponseAsync(string claimantName, string decision, string reason, decimal? amount);
}
