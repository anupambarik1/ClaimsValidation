using Claims.Domain.Entities;
using Claims.Domain.Enums;
using Claims.Services.Aws;
using Claims.Services.Interfaces;
using System.Text.RegularExpressions;

namespace Claims.Services.Implementations;

/// <summary>
/// Document analysis service using rule-based NLP techniques
/// Provides document classification, entity extraction, and validation
/// without requiring external AI services
/// </summary>
public class DocumentAnalysisService : IDocumentAnalysisService
{
    // Document type keywords for classification
    private static readonly Dictionary<string, string[]> DocumentTypeKeywords = new()
    {
        { "Invoice", new[] { "invoice", "invoice number", "invoice date", "bill to", "subtotal", "total due", "payment terms" } },
        { "Receipt", new[] { "receipt", "paid", "transaction", "payment received", "thank you for your purchase" } },
        { "MedicalReport", new[] { "medical", "diagnosis", "patient", "physician", "treatment", "prescription", "hospital", "clinic", "symptoms" } },
        { "InsurancePolicy", new[] { "policy", "coverage", "premium", "deductible", "beneficiary", "insured", "underwriter" } },
        { "ClaimForm", new[] { "claim form", "claimant", "date of loss", "description of damage", "claim number" } },
        { "PoliceReport", new[] { "police", "officer", "incident", "report number", "witness", "accident report" } },
        { "RepairEstimate", new[] { "estimate", "repair", "parts", "labor", "mechanic", "body shop", "damage assessment" } },
        { "BankStatement", new[] { "bank statement", "account", "balance", "deposit", "withdrawal", "transaction history" } }
    };

    // Suspicious patterns that might indicate fraud
    private static readonly string[] SuspiciousPatterns = new[]
    {
        @"photoshop",
        @"edited",
        @"modified",
        @"lorem ipsum",
        @"sample",
        @"test document",
        @"draft"
    };

    public async Task<string> ClassifyDocumentAsync(string extractedText)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
            return "Unknown";

        await Task.Delay(10); // Simulate processing

        var textLower = extractedText.ToLowerInvariant();
        var scores = new Dictionary<string, int>();

        // Score each document type based on keyword matches
        foreach (var (docType, keywords) in DocumentTypeKeywords)
        {
            var score = keywords.Count(keyword => textLower.Contains(keyword.ToLowerInvariant()));
            if (score > 0)
                scores[docType] = score;
        }

        // Return the type with highest score, or "Other" if no matches
        if (scores.Any())
        {
            var bestMatch = scores.OrderByDescending(s => s.Value).First();
            if (bestMatch.Value >= 2) // Require at least 2 keyword matches
                return bestMatch.Key;
        }

        return "Other";
    }

    public async Task<bool> ValidateDocumentContentAsync(Document document)
    {
        if (string.IsNullOrWhiteSpace(document.ExtractedText))
            return false;

        if (document.OcrConfidence < 0.5m)
            return false;

        await Task.Delay(10); // Simulate processing

        // Check for suspicious patterns
        var textLower = document.ExtractedText.ToLowerInvariant();
        foreach (var pattern in SuspiciousPatterns)
        {
            if (Regex.IsMatch(textLower, pattern, RegexOptions.IgnoreCase))
                return false;
        }

        // Minimum content length check
        if (document.ExtractedText.Length < 50)
            return false;

        return true;
    }

    /// <summary>
    /// Extract monetary amounts from document text
    /// </summary>
    public async Task<List<ExtractedAmount>> ExtractAmountsAsync(string text)
    {
        var amounts = new List<ExtractedAmount>();
        
        await Task.Delay(5);

        // Match various currency patterns: $1,234.56, USD 1234.56, 1,234.56 dollars
        var patterns = new[]
        {
            @"\$[\d,]+\.?\d*",                           // $1,234.56
            @"USD\s*[\d,]+\.?\d*",                       // USD 1234.56
            @"[\d,]+\.?\d*\s*(?:dollars?|USD)",         // 1234.56 dollars
            @"(?:total|amount|sum|balance):\s*\$?[\d,]+\.?\d*"  // Total: $1234.56
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                var numericPart = Regex.Match(match.Value, @"[\d,]+\.?\d*");
                if (numericPart.Success && decimal.TryParse(numericPart.Value.Replace(",", ""), out var amount))
                {
                    amounts.Add(new ExtractedAmount
                    {
                        Value = amount,
                        Context = match.Value,
                        Position = match.Index
                    });
                }
            }
        }

        return amounts.DistinctBy(a => a.Value).ToList();
    }

    /// <summary>
    /// Extract dates from document text
    /// </summary>
    public async Task<List<ExtractedDate>> ExtractDatesAsync(string text)
    {
        var dates = new List<ExtractedDate>();
        
        await Task.Delay(5);

        // Common date patterns
        var patterns = new[]
        {
            @"\d{1,2}/\d{1,2}/\d{2,4}",           // MM/DD/YYYY or M/D/YY
            @"\d{1,2}-\d{1,2}-\d{2,4}",           // MM-DD-YYYY
            @"\d{4}-\d{2}-\d{2}",                 // YYYY-MM-DD (ISO)
            @"(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+\d{1,2},?\s+\d{4}"  // January 15, 2024
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                if (DateTime.TryParse(match.Value, out var date))
                {
                    dates.Add(new ExtractedDate
                    {
                        Value = date,
                        OriginalText = match.Value,
                        Position = match.Index
                    });
                }
            }
        }

        return dates.DistinctBy(d => d.Value.Date).OrderBy(d => d.Value).ToList();
    }

    /// <summary>
    /// Calculate text similarity score between two documents (0 to 1)
    /// Used for detecting duplicate or copied documents
    /// </summary>
    public async Task<double> CalculateSimilarityAsync(string text1, string text2)
    {
        await Task.Delay(5);

        if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
            return 0.0;

        // Tokenize and normalize
        var tokens1 = TokenizeText(text1);
        var tokens2 = TokenizeText(text2);

        if (!tokens1.Any() || !tokens2.Any())
            return 0.0;

        // Jaccard similarity
        var intersection = tokens1.Intersect(tokens2).Count();
        var union = tokens1.Union(tokens2).Count();

        return union > 0 ? (double)intersection / union : 0.0;
    }

    /// <summary>
    /// Analyze document for potential fraud indicators
    /// </summary>
    public async Task<DocumentFraudAnalysis> AnalyzeForFraudAsync(Document document)
    {
        var analysis = new DocumentFraudAnalysis
        {
            DocumentId = document.DocumentId,
            RiskScore = 0m,
            Indicators = new List<string>()
        };

        await Task.Delay(10);

        if (string.IsNullOrWhiteSpace(document.ExtractedText))
        {
            analysis.RiskScore = 0.3m;
            analysis.Indicators.Add("No text extracted from document");
            return analysis;
        }

        var text = document.ExtractedText.ToLowerInvariant();

        // Check OCR confidence
        if (document.OcrConfidence < 0.5m)
        {
            analysis.RiskScore += 0.2m;
            analysis.Indicators.Add($"Low OCR confidence: {document.OcrConfidence:P0}");
        }

        // Check for suspicious patterns
        foreach (var pattern in SuspiciousPatterns)
        {
            if (text.Contains(pattern))
            {
                analysis.RiskScore += 0.3m;
                analysis.Indicators.Add($"Suspicious pattern detected: {pattern}");
            }
        }

        // Check for inconsistent dates (future dates)
        var dates = await ExtractDatesAsync(document.ExtractedText);
        if (dates.Any(d => d.Value > DateTime.UtcNow.AddDays(1)))
        {
            analysis.RiskScore += 0.25m;
            analysis.Indicators.Add("Future date detected in document");
        }

        // Cap risk score at 1.0
        analysis.RiskScore = Math.Min(analysis.RiskScore, 1.0m);
        analysis.RiskLevel = analysis.RiskScore > 0.5m ? "High" : analysis.RiskScore > 0.25m ? "Medium" : "Low";

        return analysis;
    }

    private static HashSet<string> TokenizeText(string text)
    {
        // Remove punctuation and split into words
        var cleaned = Regex.Replace(text.ToLowerInvariant(), @"[^\w\s]", " ");
        var tokens = cleaned.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Remove common stop words
        var stopWords = new HashSet<string> { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "is", "it" };
        return tokens.Where(t => t.Length > 2 && !stopWords.Contains(t)).ToHashSet();
    }

    public Task<DocumentIntelligenceResult> AnalyzeDocumentWithStructureAsync(string blobUri)
    {
        throw new NotImplementedException();
    }
}

// Supporting DTOs
public class ExtractedAmount
{
    public decimal Value { get; set; }
    public string Context { get; set; } = string.Empty;
    public int Position { get; set; }
}

public class ExtractedDate
{
    public DateTime Value { get; set; }
    public string OriginalText { get; set; } = string.Empty;
    public int Position { get; set; }
}

public class DocumentFraudAnalysis
{
    public Guid DocumentId { get; set; }
    public decimal RiskScore { get; set; }
    public string RiskLevel { get; set; } = "Low";
    public List<string> Indicators { get; set; } = new();
}
