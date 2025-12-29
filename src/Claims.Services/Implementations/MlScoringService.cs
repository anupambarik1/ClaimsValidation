using Claims.Domain.Entities;
using Claims.Services.Interfaces;
using Claims.Services.ML;
using Microsoft.ML;
using Microsoft.Extensions.Configuration;

namespace Claims.Services.Implementations;

public class MlScoringService : IMlScoringService
{
    private readonly MLContext _mlContext;
    private readonly ITransformer _model;
    private readonly PredictionEngine<ClaimInput, FraudPrediction> _predictionEngine;

    public MlScoringService(IConfiguration configuration)
    {
        _mlContext = new MLContext();

        var modelPath = configuration["MLSettings:FraudModelPath"] ?? "./MLModels/fraud-model.zip";
        var trainingDataPath = configuration["MLSettings:TrainingDataPath"] ?? "./MLModels/claims-training-data.csv";

        // Ensure model exists, train if not
        FraudModelTrainer.EnsureModelExists(modelPath, trainingDataPath);

        // Load the trained model
        _model = _mlContext.Model.Load(modelPath, out var _);
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<ClaimInput, FraudPrediction>(_model);
    }

    public async Task<(decimal FraudScore, decimal ApprovalScore)> ScoreClaimAsync(Claim claim)
    {
        // Run prediction in background task
        return await Task.Run(() =>
        {
            var input = new ClaimInput
            {
                Amount = (float)claim.TotalAmount,
                DocumentCount = claim.Documents?.Count ?? 0,
                ClaimantHistoryCount = 0, // TODO: Get from historical data
                DaysSinceLastClaim = 0 // TODO: Calculate from historical data
            };

            var prediction = _predictionEngine.Predict(input);
            
            // Fraud score is the probability from ML model
            decimal fraudScore = (decimal)prediction.Probability;
            
            // Approval score is inverse of fraud score (simplified)
            // In production, you'd have a separate approval model
            decimal approvalScore = 1.0m - fraudScore;

            return (fraudScore, approvalScore);
        });
    }

    public async Task<string> DetermineDecisionAsync(decimal fraudScore, decimal approvalScore)
    {
        await Task.Delay(10);

        // Decision thresholds
        if (fraudScore > 0.7m)
            return "Reject";

        if (approvalScore > 0.8m && fraudScore < 0.3m)
            return "AutoApprove";

        return "ManualReview";
    }
}
