using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Nets;

using GammonX.Mars.Training.Validation;

using GammonX.Models.Enums;

using static TorchSharp.torch;

namespace GammonX.Mars.Training.Sidecars;

/// <summary>
/// Contains constraint metrics for the predictions stored in a trajectory and, optionally,
/// predictions produced by a current model for the same positions.
/// </summary>
/// <param name="SourceModel">Metrics for the predictions stored in the trajectory sidecar, or <see langword="null"/> for a non-five-head mode.</param>
/// <param name="CurrentModel">Metrics for the current model's predictions, or <see langword="null"/> when no model was audited.</param>
public sealed record TrajectoryConstraintAuditResult(
    ConstraintMetricsResult? SourceModel,
    ConstraintMetricsResult? CurrentModel);

/// <summary>
/// Compares game-outcome constraint violations in recorded trajectory predictions with a current model.
/// </summary>
/// <remarks>
/// For five-head modes, the auditor always evaluates the predictions stored in the trajectory sidecar.
/// When a current model and training CSV are supplied, it also streams the matching feature rows,
/// evaluates them in batches, and reports a second set of metrics. Plakoto and Fevga use a single
/// output head, so constraint metrics are not collected for those modes.
/// </remarks>
public static class TrajectoryConstraintAuditor
{
    /// <summary>
    /// Analyzes recorded trajectory predictions and optionally compares them with a current model.
    /// </summary>
    /// <param name="modus">The game mode used to select the model architecture and output shape.</param>
    /// <param name="trajectoryCsvPath">The trajectory sidecar containing recorded predictions and positions.</param>
    /// <param name="trainingCsvPath">The training CSV containing features required for current-model evaluation.</param>
    /// <param name="currentModelPath">The current model to evaluate, or <see langword="null"/> to audit only source predictions.</param>
    /// <param name="device">The TorchSharp device on which the current model should run.</param>
    /// <param name="batchSize">The maximum number of feature rows evaluated in one model batch.</param>
    /// <returns>Source-model metrics and, when requested, current-model metrics.</returns>
    /// <exception cref="FileNotFoundException">Thrown when a required sidecar, training CSV, or model file is missing.</exception>
    /// <exception cref="InvalidDataException">Thrown when streamed rows contain inconsistent feature counts.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="batchSize"/> is not positive.</exception>
    public static TrajectoryConstraintAuditResult Analyze(
        GameModus modus,
        string trajectoryCsvPath,
        string? trainingCsvPath,
        string? currentModelPath,
        Device device,
        int batchSize = 256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trajectoryCsvPath);
        if (!File.Exists(trajectoryCsvPath))
            throw new FileNotFoundException("Trajectory sidecar was not found.", trajectoryCsvPath);

        // Only the five-head outcome representation has the hierarchy constraints being audited.
        var isFiveHeadModus = modus is not (GameModus.Plakoto or GameModus.Fevga);
        if (currentModelPath is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(trainingCsvPath);
            if (!File.Exists(trainingCsvPath))
                throw new FileNotFoundException("Training CSV was not found.", trainingCsvPath);
            if (!File.Exists(currentModelPath))
                throw new FileNotFoundException("Current model was not found.", currentModelPath);
        }

        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Batch size must be positive.");

        var sourceMetrics = isFiveHeadModus ? new ConstraintMetricsAccumulator() : null;
        var currentMetrics = isFiveHeadModus && currentModelPath is not null
            ? new ConstraintMetricsAccumulator()
            : null;

        if (currentModelPath is null)
        {
            // Without a current model, the trajectories stored predictions are the source to audit.
            foreach (var prediction in TrajectoryCsvReader.ReadPredictions(trajectoryCsvPath))
            {
                sourceMetrics?.AddRow(prediction);
            }
        }
        else
        {
            var model = NetModelFactory.CreateForModel(modus, currentModelPath, device);
            model.Eval();
            try
            {
                using var noGradScope = no_grad();
                var featureRows = new List<float[]>(batchSize);
                var featureCount = 0;

                // Read matching feature and trajectory rows incrementally to avoid loading both files in memory.
                foreach (var row in TrajectoryCsvReader.ReadRowsStreaming(
                    trainingCsvPath!,
                    trajectoryCsvPath,
                    isFiveHeadModus ? GameOutcomeConstraintValidator.FullHeadCount : 1))
                {
                    sourceMetrics?.AddRow(row.Position.Prediction);
                    if (!isFiveHeadModus)
                        continue;

                    if (featureCount == 0)
                        featureCount = row.Position.Features.Length;
                    else if (row.Position.Features.Length != featureCount)
                        throw new InvalidDataException("Trajectory audit rows contain inconsistent feature counts.");

                    featureRows.Add(row.Position.Features);
                    if (featureRows.Count >= batchSize)
                        EvaluateBatch(model, currentMetrics!, featureRows, featureCount, device);
                }

                // Evaluate the final partial batch after the streaming loop completes.
                if (featureRows.Count > 0)
                    EvaluateBatch(model, currentMetrics!, featureRows, featureCount, device);
            }
            finally
            {
                if (model is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        return new TrajectoryConstraintAuditResult(sourceMetrics?.Complete(), currentMetrics?.Complete());
    }

    /// <summary>
    /// Converts buffered feature rows into a tensor, evaluates the model, and adds its predictions to the metrics.
    /// </summary>
    /// <param name="model">The model used to produce predictions.</param>
    /// <param name="metrics">The accumulator receiving the batch metrics.</param>
    /// <param name="featureRows">The buffered feature rows to evaluate.</param>
    /// <param name="featureCount">The number of features in each row.</param>
    /// <param name="device">The device on which the input tensor is created.</param>
    private static void EvaluateBatch(
        INetModel model,
        ConstraintMetricsAccumulator metrics,
        List<float[]> featureRows,
        int featureCount,
        Device device)
    {
        // TorchSharp expects the flattened values together with their two-dimensional shape.
        var flatFeatures = new float[featureRows.Count * featureCount];
        for (var row = 0; row < featureRows.Count; row++)
        {
            featureRows[row].CopyTo(flatFeatures, row * featureCount);
        }

        using var input = tensor(flatFeatures, [featureRows.Count, featureCount], device: device);
        using var predictions = model.Forward(input);
        metrics.Add(predictions);
        featureRows.Clear();
    }
}
