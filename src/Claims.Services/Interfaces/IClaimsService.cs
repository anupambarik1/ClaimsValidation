using Claims.Domain.DTOs;
using Claims.Domain.Entities;
using Claims.Domain.Enums;

namespace Claims.Services.Interfaces;

public interface IClaimsService
{
    /// <summary>
    /// Submit a new claim for processing
    /// </summary>
    Task<ClaimResponseDto> SubmitClaimAsync(ClaimSubmissionDto submission);
    
    /// <summary>
    /// Get current status of a claim
    /// </summary>
    Task<ClaimStatusDto?> GetClaimStatusAsync(Guid claimId);
    
    /// <summary>
    /// Get all claims for a specific user
    /// </summary>
    Task<List<Claim>> GetClaimsForUserAsync(string claimantId);
    
    /// <summary>
    /// Update claim status (for manual review)
    /// </summary>
    Task<bool> UpdateClaimStatusAsync(Guid claimId, string status, string? specialistId = null);
    
    /// <summary>
    /// Process a claim through the complete AI pipeline:
    /// OCR -> Document Analysis -> Rules Validation -> ML Scoring -> Decision
    /// </summary>
    Task<ClaimProcessingResult> ProcessClaimAsync(Guid claimId);
    
    /// <summary>
    /// Add a document to an existing claim
    /// </summary>
    Task<Document> AddDocumentToClaimAsync(Guid claimId, string filePath, DocumentType documentType);
}
