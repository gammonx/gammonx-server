namespace GammonX.Mars.NN.Nets;

/// <summary>
/// Records the inputs and settings for a training run. Loaded paths are historical
/// until a caller explicitly configures another run.
/// </summary>
/// <param name="TrainCsvPath">The CSV file containing training features and target labels.</param>
/// <param name="ValCsvPath">The CSV file used to evaluate validation loss after each epoch.</param>
/// <param name="OutputModelPath">The destination for the best model checkpoint and its metadata sidecar.</param>
/// <param name="InputModelPath">
/// The parent model path for continued training, or <see langword="null"/> or empty for a new model.
/// The caller must load the parent model before starting the run.
/// </param>
/// <param name="Epochs">The maximum number of passes through the training dataset before stopping.</param>
/// <param name="BatchSize">The maximum number of samples per training or validation batch.</param>
/// <param name="ProducerCount">The number of concurrent data-loading producers for each batch stream.</param>
/// <param name="QueueCapacity">The maximum number of prepared tensor batches buffered in each batch stream's queue.</param>
/// <param name="LearningRate">The initial AdamW learning rate, which the scheduler can reduce when validation loss plateaus.</param>
/// <param name="EarlyStoppingPatience">
/// The number of consecutive epochs without a validation-loss improvement exceeding <paramref name="MinimumDelta"/>
/// before training stops.
/// </param>
/// <param name="MinimumDelta">
/// The absolute validation-loss improvement threshold used for checkpoint selection, early stopping, and learning-rate scheduling.
/// </param>
/// <param name="ShuffleLabels">Whether to randomly reassign label rows in both datasets for the noise-floor diagnostic.</param>
public sealed record NetTrainingMetadata(
    string TrainCsvPath,
    string ValCsvPath,
    string OutputModelPath,
    string? InputModelPath = null,
    int Epochs = 50,
    int BatchSize = 16384,
    int ProducerCount = 1,
    int QueueCapacity = 2,
    float LearningRate = 1e-4f,
    int EarlyStoppingPatience = 8,
    float MinimumDelta = 1e-5f,
    bool ShuffleLabels = false)
{
    internal void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(TrainCsvPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(ValCsvPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(OutputModelPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Epochs);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(BatchSize);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ProducerCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(QueueCapacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(EarlyStoppingPatience);

        if (!float.IsFinite(LearningRate) || LearningRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(LearningRate));
        if (!float.IsFinite(MinimumDelta) || MinimumDelta < 0)
            throw new ArgumentOutOfRangeException(nameof(MinimumDelta));
    }
}
