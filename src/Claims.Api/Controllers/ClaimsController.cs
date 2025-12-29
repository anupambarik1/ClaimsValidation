using Claims.Domain.DTOs;
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
}

public class UpdateClaimStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? SpecialistId { get; set; }
    public string? Comments { get; set; }
}
