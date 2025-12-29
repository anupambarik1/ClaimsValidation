using Microsoft.ML;
using Microsoft.ML.Data;

namespace Claims.Services.ML;

public class ClaimTrainingData
{
    [LoadColumn(0)]
    public float Amount { get; set; }

    [LoadColumn(1)]
    public float DocumentCount { get; set; }

    [LoadColumn(2)]
    public float ClaimantHistoryCount { get; set; }

    [LoadColumn(3)]
    public float DaysSinceLastClaim { get; set; }

    [LoadColumn(4)]
    public bool IsFraud { get; set; }
}

public class FraudPrediction
{
    [ColumnName("PredictedLabel")]
    public bool IsFraud { get; set; }

    [ColumnName("Probability")]
    public float Probability { get; set; }

    [ColumnName("Score")]
    public float Score { get; set; }
}

public class ClaimInput
{
    public float Amount { get; set; }
    public float DocumentCount { get; set; }
    public float ClaimantHistoryCount { get; set; }
    public float DaysSinceLastClaim { get; set; }
}

public class FraudModelTrainer
{
    private readonly MLContext _mlContext;
    private readonly string _modelPath;
    private readonly string _trainingDataPath;

    public FraudModelTrainer(string modelPath, string trainingDataPath)
    {
        _mlContext = new MLContext(seed: 0);
        _modelPath = modelPath;
        _trainingDataPath = trainingDataPath;
    }

    public void TrainAndSaveModel()
    {
        // Load training data
        var dataView = _mlContext.Data.LoadFromTextFile<ClaimTrainingData>(
            _trainingDataPath,
            hasHeader: true,
            separatorChar: ',');

        // Split data into training and test sets (80/20)
        var dataSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        // Build training pipeline
        var pipeline = _mlContext.Transforms
            .Concatenate("Features",
                nameof(ClaimTrainingData.Amount),
                nameof(ClaimTrainingData.DocumentCount),
                nameof(ClaimTrainingData.ClaimantHistoryCount),
                nameof(ClaimTrainingData.DaysSinceLastClaim))
            .Append(_mlContext.BinaryClassification.Trainers.FastTree(
                labelColumnName: nameof(ClaimTrainingData.IsFraud),
                numberOfLeaves: 20,
                numberOfTrees: 100,
                minimumExampleCountPerLeaf: 10,
                learningRate: 0.2));

        // Train the model
        Console.WriteLine("Training fraud detection model...");
        var model = pipeline.Fit(dataSplit.TrainSet);

        // Evaluate model
        var predictions = model.Transform(dataSplit.TestSet);
        var metrics = _mlContext.BinaryClassification.Evaluate(predictions, 
            labelColumnName: nameof(ClaimTrainingData.IsFraud));

        Console.WriteLine($"Model Metrics:");
        Console.WriteLine($"  Accuracy: {metrics.Accuracy:P2}");
        Console.WriteLine($"  AUC: {metrics.AreaUnderRocCurve:P2}");
        Console.WriteLine($"  F1 Score: {metrics.F1Score:P2}");

        // Save the model
        _mlContext.Model.Save(model, dataView.Schema, _modelPath);
        Console.WriteLine($"Model saved to: {_modelPath}");
    }

    public static void EnsureModelExists(string modelPath, string trainingDataPath)
    {
        if (!File.Exists(modelPath))
        {
            Console.WriteLine("Fraud detection model not found. Training new model...");
            var trainer = new FraudModelTrainer(modelPath, trainingDataPath);
            trainer.TrainAndSaveModel();
        }
        else
        {
            Console.WriteLine($"Using existing fraud detection model: {modelPath}");
        }
    }
}
