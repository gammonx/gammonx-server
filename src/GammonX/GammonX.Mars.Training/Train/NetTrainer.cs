using System.Diagnostics;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Nets;

using GammonX.Models.Enums;

using TorchSharp;

using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace GammonX.Mars.Training.Train;

public static class NetTrainer
{
    public static void Train(INetModel model)
    {
        var training = model.Metadata.Training
            ?? throw new ArgumentException("The model must be configured for training.", nameof(model));
        model.ConfigureTraining(training);
        var modus = model.Metadata.Modus;

        // we expect a higher learning rate for a bigger batch size
        // combination: batchSize=40960, learningRate=1e-4f (until default gen9)
        // result: 5.32M samples / 40,960 batch size ≈ 130 optimizer updates per epoch

        // combination: batchSize=16384, learningRate=1e-4f (since default gen9-1)
        // result: 5.32M samples / 16,384 batch size ≈ 325 optimizer updates per epoch

        // TODO: usual batch sizes of 32/64/128?

        // TODO: support 5 head output for Plakoto and Fevga
        var labelCount = (modus == GameModus.Fevga || modus == GameModus.Plakoto) ? 1 : 5;

        var device = cuda.is_available() ? CUDA : CPU;

        model.MoveTo(device);

        var trainStopwatch = Stopwatch.StartNew();

        // we convert the csv files to a binary format for faster training, and build an index of row offsets for random access
        var trainBinaryPath = $"{Path.GetFileNameWithoutExtension(training.TrainCsvPath)}.bin";
        var (trainFeatureCount, trainRowCount, _) = BinaryBatchEnumerator.ScanCsvAndConvertToBinary(training.TrainCsvPath, trainBinaryPath, labelCount);
        
        var valBinaryPath = $"{Path.GetFileNameWithoutExtension(training.ValCsvPath)}.bin";
        var (valFeatureCount, valRowCount, _) = BinaryBatchEnumerator.ScanCsvAndConvertToBinary(training.ValCsvPath, valBinaryPath, labelCount);

        Console.WriteLine($"Device: {device}");

        int[]? labelPermutation = null;
        if (training.ShuffleLabels)
        {
            Console.WriteLine("WARNING: label shuffling enabled, not a real training run.");
            labelPermutation = Enumerable.Range(0, trainRowCount).OrderBy(_ => Random.Shared.Next()).ToArray();
        }

        using var optimizer = optim.AdamW(model.GetParameters(), lr: training.LearningRate, weight_decay: 1e-4);
        var scheduler = optim.lr_scheduler.ReduceLROnPlateau(
            optimizer,
            factor: 0.66,
            patience: 3,
            threshold: training.MinimumDelta,
            threshold_mode: "abs");

        // Reduced BCE trains the model; unreduced BCE supplies row-weighted per-head diagnostics.
        using var loss = BCELoss();
        using var diagnosticLoss = BCELoss(reduction: Reduction.None);

        Console.WriteLine($"Train={trainRowCount}  Val={valRowCount}");

        var valOrder = Enumerable.Range(0, valRowCount).ToArray();
        int[]? valLabelPerm = training.ShuffleLabels
            ? Enumerable.Range(0, valRowCount).OrderBy(_ => Random.Shared.Next()).ToArray()
            : null;

        var epochsWithoutImprovement = 0;
        var bestValLoss = float.MaxValue;
        var bestTrainLoss = float.MaxValue;
        var bestEpoch = 0;

        if (!string.IsNullOrEmpty(training.InputModelPath))
        {
            // we are continuing training an existing model, so we need to evaluate it first to get a baseline for early stopping
            var parentValBatches = new BinaryBatchEnumerator(
                valBinaryPath,
                training.BatchSize,
                labelCount,
                valFeatureCount,
                valOrder,
                device,
                valLabelPerm,
                training.ProducerCount,
                training.QueueCapacity);

            model.Eval();

            using (no_grad())
            {
                var parentMetrics = RunEpochStreaming(
                    model,
                    optimizer,
                    loss,
                    diagnosticLoss,
                    parentValBatches,
                    labelCount,
                    false);

                bestValLoss = parentMetrics.Loss;
                scheduler.step(bestValLoss);
            }
            model.Save(training.OutputModelPath);
            Console.WriteLine($"Parent val_loss={bestValLoss:F5}");
        }

        for (var epoch = 1; epoch <= training.Epochs; epoch++)
        {
            var epochStopwatch = Stopwatch.StartNew();

            // We shuffle training row order each epoch for better convergence
            var trainOrder = Enumerable.Range(0, trainRowCount).OrderBy(_ => Random.Shared.Next()).ToArray();

            var trainBatches = new BinaryBatchEnumerator(trainBinaryPath, training.BatchSize, labelCount, trainFeatureCount, trainOrder, device, labelPermutation, training.ProducerCount, training.QueueCapacity);
            var valBatches = new BinaryBatchEnumerator(valBinaryPath, training.BatchSize, labelCount, valFeatureCount, valOrder, device, valLabelPerm, training.ProducerCount, training.QueueCapacity);

            model.Train();
            var trainMetrics = RunEpochStreaming(model, optimizer, loss, diagnosticLoss, trainBatches, labelCount, true);

            model.Eval();
            EpochMetricsResult valMetrics;
            using (no_grad())
                valMetrics = RunEpochStreaming(model, optimizer, loss, diagnosticLoss, valBatches, labelCount, false);

            var currentLr = optimizer.ParamGroups.First().LearningRate;

            if (valMetrics.Loss < bestValLoss - training.MinimumDelta)
            {
                bestValLoss = valMetrics.Loss;
                bestTrainLoss = trainMetrics.Loss;
                bestEpoch = epoch;
                epochsWithoutImprovement = 0;
                model.Save(training.OutputModelPath);
            }
            else
            {
                epochsWithoutImprovement++;
            }

            epochStopwatch.Stop();

            Console.WriteLine("##############################################");
            var marker = epoch == bestEpoch ? " OK" : "";
            Console.WriteLine($@"Epoch {epoch,3}/{training.Epochs}  lr={currentLr:G3}  elapsed={epochStopwatch.Elapsed:hh\:mm\:ss}{marker}");
            // We log the train/val loss and accuracy metrics for each head, as well as the overall loss and accuracy
            Console.WriteLine($"  train_loss={trainMetrics.Loss:F5} [{FormatOutputLosses(trainMetrics.PerOutputLosses, labelCount)}]");
            Console.WriteLine($"  val_loss={valMetrics.Loss:F5} [{FormatOutputLosses(valMetrics.PerOutputLosses, labelCount)}]");

            scheduler.step(valMetrics.Loss);

            if (epochsWithoutImprovement >= training.EarlyStoppingPatience)
            {
                Console.WriteLine($"Early stopping — best val_loss={bestValLoss:F5} train_loss={bestTrainLoss:F5} val_gap={bestValLoss-bestTrainLoss:F5} at epoch {bestEpoch}");
                break;
            }
        }

        trainStopwatch.Stop();
        Console.WriteLine($@"Training elapsed time: {trainStopwatch.Elapsed:hh\:mm\:ss}");

        Console.WriteLine($"Model saved: {training.OutputModelPath}  (epoch {bestEpoch}  val_loss: {bestValLoss:F5} train_loss: {bestTrainLoss:F5})");
    }
        
    private static EpochMetricsResult RunEpochStreaming(
        INetModel model,
        optim.Optimizer optimizer,
        Loss<Tensor, Tensor, Tensor> loss,
        Loss<Tensor, Tensor, Tensor> diagnosticLoss,
        IEnumerable<(Tensor features, Tensor labels)> batches,
        int outputCount,
        bool train)
    {
        using var metrics = new EpochMetricsAccumulator(
            outputCount,
            collectConstraintMetrics: outputCount == GameOutcomeConstraintValidator.FullHeadCount);
        
        foreach (var (xBatch, yBatch) in batches)
        {
            using (xBatch)
            using (yBatch)
            {
                // forward propagation
                using var pred = model.Forward(xBatch);
                using var l = loss.forward(pred, yBatch);

                if (train)
                {
                    // set optimizer to zero grad to remove previous epoch gradients
                    optimizer.zero_grad();
                    // backward propagation
                    l.backward();
                    // optimize weights
                    optimizer.step();
                }

                using (no_grad())
                {
                    using var batchElementLosses = diagnosticLoss.forward(pred, yBatch);
                    using var batchLossSums = outputCount == 1
                        ? batchElementLosses.sum().reshape(1)
                        : batchElementLosses.sum(dim: 0);
                    metrics.Add(batchLossSums, yBatch.shape[0], pred);
                }
            }
        }

        return metrics.Complete();
    }

    private static string FormatOutputLosses(IReadOnlyList<float> losses, int outputCount)
    {
        string[] names = outputCount == 1
            ? ["pWin"]
            : ["pWin", "pGW", "pBgW", "pGL", "pBgL"];

        return string.Join(", ", names.Zip(losses, (name, value) => $"{name}={value:F5}"));
    }
}
