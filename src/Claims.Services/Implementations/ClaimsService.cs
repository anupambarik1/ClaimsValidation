using Claims.Domain.DTOs;
using Claims.Domain.Entities;
using Claims.Domain.Enums;
using Claims.Infrastructure.Data;
using Claims.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Claims.Services.Implementations;

public class ClaimsService : IClaimsService
{
    private readonly ClaimsDbContext _context;
    private readonly IOcrService _ocrService;
    private readonly IRulesEngineService _rulesEngineService;
    private readonly IMlScoringService _mlScoringService;
    private readonly INotificationService _notificationService;
    private readonly IDocumentAnalysisService _documentAnalysisService;
    private readonly INlpService _nlpService;
    private readonly ILogger<ClaimsService> _logger;

    public ClaimsService(
        ClaimsDbContext context,
        IOcrService ocrService,
        IRulesEngineService rulesEngineService,
        IMlScoringService mlScoringService,
        INotificationService notificationService,
        IDocumentAnalysisService documentAnalysisService,
        INlpService nlpService,
        ILogger<ClaimsService> logger)
    {
        _context = context;
        _ocrService = ocrService;
        _rulesEngineService = rulesEngineService;
        _mlScoringService = mlScoringService;
        _notificationService = notificationService;
        _documentAnalysisService = documentAnalysisService;
        _nlpService = nlpService;
        _logger = logger;
    }

    public async Task<ClaimResponseDto> SubmitClaimAsync(ClaimSubmissionDto submission)
    {
        var claim = new Claim
        {
            ClaimId = Guid.NewGuid(),
            PolicyId = submission.PolicyId,
            ClaimantId = submission.ClaimantId,
            TotalAmount = submission.TotalAmount,
            Status = ClaimStatus.Submitted,
            SubmittedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            Documents = new List<Document>()
        };

        // Add documents to the claim
        foreach (var doc in submission.Documents)
        {
            var document = new Document
            {
                DocumentId = Guid.NewGuid(),
                ClaimId = claim.ClaimId,
                DocumentType = doc.DocumentType,
                BlobUri = doc.FilePath, // Use original file path for Textract/OCR processing
                UploadedDate = DateTime.UtcNow,
                OcrStatus = OcrStatus.Pending
            };
            
            claim.Documents.Add(document);
            _logger.LogInformation("Document added to claim {ClaimId}: Type={DocumentType}, Path={FilePath}", 
                claim.ClaimId, doc.DocumentType, doc.FilePath);
        }

        _context.Claims.Add(claim);
        await _context.SaveChangesAsync();

        // Send notification
        await _notificationService.SendClaimStatusNotificationAsync(
            claim.ClaimId,
            submission.ClaimantId,
            NotificationType.ClaimReceived);

        return new ClaimResponseDto
        {
            ClaimId = claim.ClaimId,
            Status = claim.Status,
            SubmittedDate = claim.SubmittedDate,
            Message = "Claim submitted successfully"
        };
    }

    /// <summary>
    /// Process a claim through the complete AI pipeline:
    /// 1. OCR - Extract text from documents
    /// 2. Document Analysis - Classify and validate documents
    /// 3. Rules Engine - Validate against business rules
    /// 4. ML Scoring - Calculate fraud and approval scores
    /// 5. Decision - Auto-approve, reject, or route to manual review
    /// </summary>
    public async Task<ClaimProcessingResult> ProcessClaimAsync(Guid claimId)
    {
        var result = new ClaimProcessingResult { ClaimId = claimId };
        
        try
        {

            // Step 2: Classify document
            var sampleClaimsPDFExtractedTextByAWSTextract = await _documentAnalysisService.AnalyzeDocumentWithStructureAsync("s3://claims-documents-validation/claims_invoice.pdf");


            var claim = await _context.Claims
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null)
            {
                result.Success = false;
                result.ErrorMessage = "Claim not found";
                return result;
            }

            _logger.LogInformation("Starting AI processing for claim {ClaimId}", claimId);
            
            // Update status to processing
            claim.Status = ClaimStatus.Processing;
            claim.LastUpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Step 1: Process documents with OCR
            _logger.LogInformation("Step 1: OCR Processing for {DocumentCount} documents", claim.Documents.Count);
            var ocrResults = new List<DocumentOcrResult>();
            foreach (var document in claim.Documents)
            {
                try
                {
                    var (extractedText, confidence) = await _ocrService.ProcessDocumentAsync(document.BlobUri);
                    
                    document.ExtractedText = extractedText;
                    document.OcrConfidence = confidence;
                    document.OcrStatus = confidence >= 0.7m ? OcrStatus.Completed : OcrStatus.Failed;
                    
                    // Step 2: Classify document
                    var docType = await _documentAnalysisService.ClassifyDocumentAsync(extractedText);
                    
                    ocrResults.Add(new DocumentOcrResult
                    {
                        DocumentId = document.DocumentId,
                        ExtractedText = extractedText,
                        Confidence = confidence,
                        ClassifiedType = docType,
                        Success = !string.IsNullOrEmpty(extractedText)
                    });
                    
                    _logger.LogInformation("Document {DocumentId} processed: Type={Type}, Confidence={Confidence:P2}", 
                        document.DocumentId, docType, confidence);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OCR failed for document {DocumentId}", document.DocumentId);
                    document.OcrStatus = OcrStatus.Failed;
                    ocrResults.Add(new DocumentOcrResult
                    {
                        DocumentId = document.DocumentId,
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                }
            }
            result.OcrResults = ocrResults;
            await _context.SaveChangesAsync();

            // Step 3: Validate against business rules
            _logger.LogInformation("Step 2: Business Rules Validation");
            var (isValid, validationReason) = await _rulesEngineService.ValidatePolicyAsync(claim);
            result.RulesValidation = new RulesValidationResult
            {
                IsValid = isValid,
                Reason = validationReason,
                RulesChecked = new List<string> { "PolicyLimit", "PolicyValidity", "CoverageCheck" }
            };

            if (!isValid)
            {
                _logger.LogWarning("Claim {ClaimId} failed rules validation: {Reason}", claimId, validationReason);
                claim.Status = ClaimStatus.Rejected;
                claim.LastUpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Create decision record
                await CreateDecisionAsync(claim, DecisionStatus.Rejected, validationReason ?? "Failed policy validation");
                
                result.FinalDecision = "Rejected";
                result.DecisionReason = validationReason;
                result.Success = true;
                return result;
            }

            // Step 2.5: NLP Analysis with Bedrock
            _logger.LogInformation("Step 2.5: NLP Analysis");
            var ocrText = ocrResults.FirstOrDefault()?.ExtractedText ?? string.Empty;
            var summary = await _nlpService.SummarizeClaimAsync(
                claim.TotalAmount.ToString(),
                ocrText);

            var fraudNarrativeJson = await _nlpService.AnalyzeFraudNarrativeAsync(summary);
            var fraudNarrative = JsonSerializer.Deserialize<JsonElement>(fraudNarrativeJson);
            var narrativeFraudScore = (decimal)fraudNarrative.GetProperty("riskScore").GetDouble();

            var entitiesJson = await _nlpService.ExtractEntitiesAsync(ocrText);

            result.NlpAnalysis = new NlpAnalysisResult
            {
                Summary = summary,
                FraudRiskScore = narrativeFraudScore,
                DetectedEntities = entitiesJson
            };
            
            _logger.LogInformation("NLP Analysis completed: Fraud Risk Score={FraudRiskScore:P2}", narrativeFraudScore);

            // Step 4: ML-based fraud and approval scoring
            _logger.LogInformation("Step 3: ML Fraud Detection and Scoring");
            var (fraudScore, approvalScore) = await _mlScoringService.ScoreClaimAsync(claim);
            
            // Combine NLP and ML fraud scores (60% ML, 40% NLP)
            var combinedFraudScore = (fraudScore * 0.6m) + (narrativeFraudScore * 0.4m);
            claim.FraudScore = combinedFraudScore;
            claim.ApprovalScore = approvalScore;
            
            result.MlScoring = new MlScoringResult
            {
                FraudScore = combinedFraudScore,
                ApprovalScore = approvalScore,
                FraudRiskLevel = combinedFraudScore > 0.7m ? "High" : combinedFraudScore > 0.4m ? "Medium" : "Low"
            };
            
            _logger.LogInformation("Claim {ClaimId} scored: FraudScore={FraudScore:P2} (ML={MlScore:P2} + NLP={NlpScore:P2}), ApprovalScore={ApprovalScore:P2}", 
                claimId, combinedFraudScore, fraudScore, narrativeFraudScore, approvalScore);

            // Step 5: Determine final decision
            var decision = await _mlScoringService.DetermineDecisionAsync(combinedFraudScore, approvalScore);
            result.FinalDecision = decision;

            switch (decision)
            {
                case "AutoApprove":
                    claim.Status = ClaimStatus.Approved;
                    result.DecisionReason = $"Auto-approved: Low fraud risk ({combinedFraudScore:P2}), high approval score ({approvalScore:P2})";
                    await CreateDecisionAsync(claim, DecisionStatus.Approved, result.DecisionReason, isAutoDecision: true);
                    _logger.LogInformation("Claim {ClaimId} auto-approved", claimId);
                    break;

                case "Reject":
                    claim.Status = ClaimStatus.Rejected;
                    result.DecisionReason = $"Rejected: High fraud risk detected ({combinedFraudScore:P2})";
                    await CreateDecisionAsync(claim, DecisionStatus.Rejected, result.DecisionReason, isAutoDecision: true);
                    _logger.LogWarning("Claim {ClaimId} rejected due to high fraud score", claimId);
                    break;

                default: // ManualReview
                    claim.Status = ClaimStatus.UnderReview;
                    result.DecisionReason = $"Requires manual review: Moderate risk (Fraud: {combinedFraudScore:P2}, Approval: {approvalScore:P2})";
                    await CreateDecisionAsync(claim, DecisionStatus.PendingReview, result.DecisionReason);
                    _logger.LogInformation("Claim {ClaimId} routed to manual review", claimId);
                    break;
            }

            claim.LastUpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Send notification about decision
            await _notificationService.SendClaimStatusNotificationAsync(
                claim.ClaimId,
                claim.ClaimantId,
                NotificationType.DecisionMade);

            result.Success = true;
            result.ProcessingTimeMs = (DateTime.UtcNow - claim.SubmittedDate).TotalMilliseconds;
            
            _logger.LogInformation("Claim {ClaimId} processing completed: {Decision}", claimId, decision);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing claim {ClaimId}", claimId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    private async Task CreateDecisionAsync(Claim claim, DecisionStatus status, string reason, bool isAutoDecision = false)
    {
        var decision = new Decision
        {
            DecisionId = Guid.NewGuid(),
            ClaimId = claim.ClaimId,
            Status = status,
            DecisionDate = DateTime.UtcNow,
            Reason = reason,
            ReviewerId = isAutoDecision ? "AI_SYSTEM" : null
        };

        _context.Decisions.Add(decision);
        await _context.SaveChangesAsync();
    }

    public async Task<Document> AddDocumentToClaimAsync(Guid claimId, string filePath, DocumentType documentType)
    {
        var claim = await _context.Claims.FirstOrDefaultAsync(c => c.ClaimId == claimId);
        if (claim == null)
            throw new ArgumentException("Claim not found", nameof(claimId));

        var document = new Document
        {
            DocumentId = Guid.NewGuid(),
            ClaimId = claimId,
            BlobUri = filePath,
            DocumentType = documentType,
            UploadedDate = DateTime.UtcNow,
            OcrStatus = OcrStatus.Pending
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return document;
    }

    public async Task<ClaimStatusDto?> GetClaimStatusAsync(Guid claimId)
    {
        var claim = await _context.Claims
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);

        if (claim == null)
            return null;

        return new ClaimStatusDto
        {
            ClaimId = claim.ClaimId,
            Status = claim.Status,
            SubmittedDate = claim.SubmittedDate,
            LastUpdatedDate = claim.LastUpdatedDate,
            FraudScore = claim.FraudScore,
            ApprovalScore = claim.ApprovalScore,
            AssignedSpecialistId = claim.AssignedSpecialistId
        };
    }

    public async Task<List<Claim>> GetClaimsForUserAsync(string claimantId)
    {
        return await _context.Claims
            .Where(c => c.ClaimantId == claimantId)
            .OrderByDescending(c => c.SubmittedDate)
            .ToListAsync();
    }

    public async Task<bool> UpdateClaimStatusAsync(Guid claimId, string status, string? specialistId = null)
    {
        var claim = await _context.Claims.FirstOrDefaultAsync(c => c.ClaimId == claimId);
        
        if (claim == null)
            return false;

        if (Enum.TryParse<ClaimStatus>(status, out var claimStatus))
        {
            claim.Status = claimStatus;
            claim.LastUpdatedDate = DateTime.UtcNow;
            
            if (!string.IsNullOrEmpty(specialistId))
                claim.AssignedSpecialistId = specialistId;

            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }
}
