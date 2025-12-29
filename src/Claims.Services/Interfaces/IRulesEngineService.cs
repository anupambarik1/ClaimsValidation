using Claims.Domain.Entities;

namespace Claims.Services.Interfaces;

public interface IRulesEngineService
{
    Task<(bool IsValid, string? Reason)> ValidatePolicyAsync(Claim claim);
    Task<bool> CheckCoverageAsync(string policyId, decimal amount);
}
