namespace Claims.Domain.DTOs;

/// <summary>
/// Complete result of AI-powered claim processing pipeline
/// </summary>
public class ClaimProcessingResult
{
    public Guid ClaimId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Final decision: AutoApprove, Reject, or ManualReview
    /// </summary>
    public string FinalDecision { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable explanation of the decision
    /// </summary>
    public string? DecisionReason { get; set; }
    
    /// <summary>
    /// Results from OCR processing of all documents
    /// </summary>
    public List<DocumentOcrResult> OcrResults { get; set; } = new();
    
    /// <summary>
    /// Results from business rules validation
    /// </summary>
    public RulesValidationResult? RulesValidation { get; set; }
    
    /// <summary>
    /// Results from NLP analysis (Bedrock + Comprehend)
    /// </summary>
    public NlpAnalysisResult? NlpAnalysis { get; set; }
    
    /// <summary>
    /// Results from ML-based fraud detection and scoring
    /// </summary>
    public MlScoringResult? MlScoring { get; set; }
    
    /// <summary>
    /// Total processing time in milliseconds
    /// </summary>
    public double ProcessingTimeMs { get; set; }
}

/// <summary>
/// OCR processing result for a single document
/// </summary>
public class DocumentOcrResult
{
    public Guid DocumentId { get; set; }
    public bool Success { get; set; }
    public string? ExtractedText { get; set; }
    public decimal Confidence { get; set; }
    public string? ClassifiedType { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Business rules validation result
/// </summary>
public class RulesValidationResult
{
    public bool IsValid { get; set; }
    public string? Reason { get; set; }
    public List<string> RulesChecked { get; set; } = new();
    public Dictionary<string, bool> RuleResults { get; set; } = new();
}

/// <summary>
/// NLP analysis result using AWS Bedrock and Comprehend
/// </summary>
public class NlpAnalysisResult
{
    /// <summary>
    /// Summarized claim description from Bedrock
    /// </summary>
    public string? Summary { get; set; }
    
    /// <summary>
    /// Fraud risk score from NLP analysis (0.0 to 1.0)
    /// </summary>
    public decimal FraudRiskScore { get; set; }
    
    /// <summary>
    /// Extracted entities (JSON string) from Comprehend + Bedrock
    /// </summary>
    public string? DetectedEntities { get; set; }
    
    /// <summary>
    /// Claim type classification from Bedrock
    /// </summary>
    public string? ClaimType { get; set; }
}

/// <summary>
/// ML-based scoring result
/// </summary>
public class MlScoringResult
{
    /// <summary>
    /// Probability that the claim is fraudulent (0.0 to 1.0)
    /// Combined score: 60% ML + 40% NLP
    /// </summary>
    public decimal FraudScore { get; set; }
    
    /// <summary>
    /// Probability that the claim should be approved (0.0 to 1.0)
    /// </summary>
    public decimal ApprovalScore { get; set; }
    
    /// <summary>
    /// Risk level: Low, Medium, High
    /// </summary>
    public string FraudRiskLevel { get; set; } = string.Empty;
    
    /// <summary>
    /// Factors that contributed to the fraud score
    /// </summary>
    public List<string> RiskFactors { get; set; } = new();
}
