using Claims.Domain.DTOs;
using Claims.Domain.Enums;
using Claims.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClaimsController : ControllerBase
{
    private readonly IClaimsService _claimsService;
    private readonly ILogger<ClaimsController> _logger;

    public ClaimsController(IClaimsService claimsService, ILogger<ClaimsController> logger)
    {
        _claimsService = claimsService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a new claim
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ClaimResponseDto>> SubmitClaim([FromBody] ClaimSubmissionDto submission)
    {
        try
        {
            var result = await _claimsService.SubmitClaimAsync(submission);
            return CreatedAtAction(nameof(GetClaimStatus), new { claimId = result.ClaimId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting claim");
            return StatusCode(500, "An error occurred while submitting the claim");
        }
    }

    /// <summary>
    /// Get claim status by ID
    /// </summary>
    [HttpGet("{claimId}/status")]
    public async Task<ActionResult<ClaimStatusDto>> GetClaimStatus(Guid claimId)
    {
        var status = await _claimsService.GetClaimStatusAsync(claimId);
        
        if (status == null)
            return NotFound($"Claim {claimId} not found");

        return Ok(status);
    }

    /// <summary>
    /// Get all claims for a user
    /// </summary>
    [HttpGet("user/{claimantId}")]
    public async Task<ActionResult> GetUserClaims(string claimantId)
    {
        var claims = await _claimsService.GetClaimsForUserAsync(claimantId);
        return Ok(claims);
    }

    /// <summary>
    /// Update claim status (for manual review)
    /// </summary>
    [HttpPut("{claimId}/status")]
    public async Task<ActionResult> UpdateClaimStatus(
        Guid claimId,
        [FromBody] UpdateClaimStatusRequest request)
    {
        var result = await _claimsService.UpdateClaimStatusAsync(
            claimId,
            request.Status,
            request.SpecialistId);

        if (!result)
            return NotFound($"Claim {claimId} not found");

        return Ok(new { message = "Claim status updated successfully" });
    }

    /// <summary>
    /// Process a claim through the complete AI pipeline
    /// This triggers: OCR → Document Analysis → Rules Validation → ML Scoring → Decision
    /// </summary>
    [HttpPost("{claimId}/process")]
    public async Task<ActionResult<ClaimProcessingResult>> ProcessClaim(Guid claimId)
    {
        try
        {
            _logger.LogInformation("Starting AI processing for claim {ClaimId}", claimId);
            
            var result = await _claimsService.ProcessClaimAsync(claimId);
            
            if (!result.Success)
            {
                _logger.LogWarning("Claim processing failed: {Error}", result.ErrorMessage);
                return BadRequest(result);
            }

            _logger.LogInformation("Claim {ClaimId} processed successfully: {Decision}", 
                claimId, result.FinalDecision);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing claim {ClaimId}", claimId);
            return StatusCode(500, new { error = "An error occurred while processing the claim" });
        }
    }

    /// <summary>
    /// Add a document to a claim
    /// </summary>
    [HttpPost("{claimId}/documents")]
    public async Task<ActionResult> AddDocument(
        Guid claimId,
        [FromBody] AddDocumentRequest request)
    {
        try
        {
            if (!Enum.TryParse<DocumentType>(request.DocumentType, true, out var docType))
            {
                return BadRequest($"Invalid document type. Valid types: {string.Join(", ", Enum.GetNames<DocumentType>())}");
            }

            var document = await _claimsService.AddDocumentToClaimAsync(
                claimId,
                request.FilePath,
                docType);

            return Ok(new
            {
                documentId = document.DocumentId,
                claimId = document.ClaimId,
                documentType = document.DocumentType.ToString(),
                uploadedDate = document.UploadedDate,
                message = "Document added successfully"
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding document to claim {ClaimId}", claimId);
            return StatusCode(500, "An error occurred while adding the document");
        }
    }

    /// <summary>
    /// Submit and immediately process a claim (convenience endpoint)
    /// Combines SubmitClaim + ProcessClaim into a single call
    /// </summary>
    [HttpPost("submit-and-process")]
    public async Task<ActionResult<ClaimProcessingResult>> SubmitAndProcess(
        [FromBody] ClaimSubmissionDto submission)
    {
        try
        {
            // Step 1: Submit the claim
            var submitResult = await _claimsService.SubmitClaimAsync(submission);
            _logger.LogInformation("Claim {ClaimId} submitted, starting processing", submitResult.ClaimId);

            // Step 2: Process the claim through AI pipeline
            var processResult = await _claimsService.ProcessClaimAsync(submitResult.ClaimId);
            
            return Ok(processResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in submit-and-process");
            return StatusCode(500, new { error = "An error occurred while processing the claim" });
        }
    }

    
}

public class UpdateClaimStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? SpecialistId { get; set; }
    public string? Comments { get; set; }
}

public class AddDocumentRequest
{
    public string FilePath { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "Other";
}
