using System.Diagnostics;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Nets;
using GammonX.Mars.Training.Validation;
using GammonX.Models.Enums;

using TorchSharp;

using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace GammonX.Mars.Training.Train;

public static class NetTrainer
{
    public static void Train(
        GameModus modus,
        string trainCsvPath,
        string valCsvPath,
        string? inputModelPath,
        string outputModelPath,
        int epochs = 100,
        int batchSize = 4096,
        int producerCount = 1,
        int queueCapacity = 2,
        float learningRate = 1e-4f,
        int earlyStoppingPatience = 11,
        bool shuffleLabels = false,
        GameOutcomeOutputMode outputMode = GameOutcomeOutputMode.MonotonicCumulative,
        NetArchitecture architecture = NetArchitecture.A)
    {
        // TODO: enable full GAME equity predictions for plakoto/fevga
        var labelCount = (modus == GameModus.Fevga || modus == GameModus.Plakoto) ? 1 : 5;

        var device = cuda.is_available() ? CUDA : CPU;

        INetModel model;

        if (!string.IsNullOrEmpty(inputModelPath))
        {
            // we load an existing model to continue training
            model = NetModelFactory.CreateForModel(modus, inputModelPath, device);
        }
        else
        {
            // we create a new model from scratch
            model = NetModelFactory.CreateNew(modus, device, outputMode, architecture);
        }

        model.MoveTo(device);

        var trainStopwatch = Stopwatch.StartNew();

        // we convert the csv files to a binary format for faster training, and build an index of row offsets for random access
        var trainBinaryPath = $"{Path.GetFileNameWithoutExtension(trainCsvPath)}.bin";
        var (trainFeatureCount, trainRowCount, _) = BinaryBatchEnumerator.ScanCsvAndConvertToBinary(trainCsvPath, trainBinaryPath, labelCount);
        
        var valBinaryPath = $"{Path.GetFileNameWithoutExtension(valCsvPath)}.bin";
        var (valFeatureCount, valRowCount, _) = BinaryBatchEnumerator.ScanCsvAndConvertToBinary(valCsvPath, valBinaryPath, labelCount);

        Console.WriteLine($"Device: {device}");

        int[]? labelPermutation = null;
        if (shuffleLabels)
        {
            Console.WriteLine("WARNING: label shuffling enabled, not a real training run.");
            labelPermutation = Enumerable.Range(0, trainRowCount).OrderBy(_ => Random.Shared.Next()).ToArray();
        }

        var optimizer = optim.Adam(model.GetParameters(), lr: learningRate, weight_decay: 5e-4);
        var scheduler = optim.lr_scheduler.StepLR(optimizer, step_size: 10, gamma: 0.66);
        // Reduced BCE trains the model; unreduced BCE supplies row-weighted per-head diagnostics.
        var loss = BCELoss();
        var diagnosticLoss = BCELoss(reduction: Reduction.None);

        Console.WriteLine($"Train={trainRowCount}  Val={valRowCount}");

        var valOrder = Enumerable.Range(0, valRowCount).ToArray();
        int[]? valLabelPerm = shuffleLabels
            ? Enumerable.Range(0, valRowCount).OrderBy(_ => Random.Shared.Next()).ToArray()
            : null;

        var epochsWithoutImprovement = 0;
        var bestValLoss = float.MaxValue;
        var bestEpoch = 0;

        if (!string.IsNullOrEmpty(inputModelPath))
        {
            // we are continuing training an existing model, so we need to evaluate it first to get a baseline for early stopping
            var parentValBatches = new BinaryBatchEnumerator(
                valBinaryPath,
                batchSize,
                labelCount,
                valFeatureCount,
                valOrder,
                device,
                valLabelPerm,
                producerCount,
                queueCapacity);

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
            }
            model.Save(outputModelPath);
            Console.WriteLine($"Parent val_loss={bestValLoss:F5}");
        }

        for (var epoch = 1; epoch <= epochs; epoch++)
        {
            var epochStopwatch = Stopwatch.StartNew();

            // We shuffle training row order each epoch for better convergence
            var trainOrder = Enumerable.Range(0, trainRowCount).OrderBy(_ => Random.Shared.Next()).ToArray();

            var trainBatches = new BinaryBatchEnumerator(trainBinaryPath, batchSize, labelCount, trainFeatureCount, trainOrder, device, labelPermutation, producerCount, queueCapacity);
            var valBatches = new BinaryBatchEnumerator(valBinaryPath, batchSize, labelCount, valFeatureCount, valOrder, device, valLabelPerm, producerCount, queueCapacity);

            model.Train();
            var trainMetrics = RunEpochStreaming(model, optimizer, loss, diagnosticLoss, trainBatches, labelCount, true);

            model.Eval();
            EpochMetricsResult valMetrics;
            using (no_grad())
                valMetrics = RunEpochStreaming(model, optimizer, loss, diagnosticLoss, valBatches, labelCount, false);

            var currentLr = optimizer.ParamGroups.First().LearningRate;

            if (valMetrics.Loss < bestValLoss)
            {
                bestValLoss = valMetrics.Loss;
                bestEpoch = epoch;
                epochsWithoutImprovement = 0;
                model.Save(outputModelPath);
                NetModelMetadata.Write(outputModelPath, modus, outputMode, architecture);
            }
            else
            {
                epochsWithoutImprovement++;
            }

            epochStopwatch.Stop();

            Console.WriteLine("##############################################");
            var marker = epoch == bestEpoch ? " OK" : "";
            Console.WriteLine($@"Epoch {epoch,3}/{epochs}  lr={currentLr:G3}  elapsed={epochStopwatch.Elapsed:hh\:mm\:ss}{marker}");
            // We log the train/val loss and accuracy metrics for each head, as well as the overall loss and accuracy
            Console.WriteLine($"  train_loss={trainMetrics.Loss:F5} [{FormatOutputLosses(trainMetrics.PerOutputLosses, labelCount)}]");
            Console.WriteLine($"  val_loss={valMetrics.Loss:F5} [{FormatOutputLosses(valMetrics.PerOutputLosses, labelCount)}]");

            // enable if output constraint metrics are needed for debugging
            //if (trainMetrics.ConstraintMetrics != null && valMetrics.ConstraintMetrics != null)
            //{
            //    // We log metrics for the constraint validator, which checks that the model outputs are consistent with the game rules
            //    Console.WriteLine("Constraint Metrics:");
            //    Console.WriteLine($"  train: {FormatConstraintMetrics(trainMetrics.ConstraintMetrics)}");
            //    Console.WriteLine($"  val  : {FormatConstraintMetrics(valMetrics.ConstraintMetrics)}");
            //}

            if (epochsWithoutImprovement >= earlyStoppingPatience)
            {
                Console.WriteLine($"Early stopping — best val_loss={bestValLoss:F5} at epoch {bestEpoch}");
                break;
            }

            scheduler.step();
        }

        trainStopwatch.Stop();
        Console.WriteLine($@"Training elapsed time: {trainStopwatch.Elapsed:hh\:mm\:ss}");

        Console.WriteLine($"Model saved: {outputModelPath}  (epoch {bestEpoch}  val_loss: {bestValLoss:F5})");
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
                using var pred = model.Forward(xBatch);
                using var l = loss.forward(pred, yBatch);

                if (train)
                {
                    optimizer.zero_grad();
                    l.backward();
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

    private static string FormatConstraintMetrics(ConstraintMetricsResult metrics)
    {
        return $"invalid={metrics.InvalidRowCount}/{metrics.RowCount} ({metrics.InvalidRate:P2}), " +
            $"rules={metrics.RuleViolationCount}, avgSev={metrics.AverageSeverity:G4}, maxSev={metrics.MaxSeverity:G4}, " +
            $"range={metrics.RangeViolationRows}, GW={metrics.WinGammonHierarchyViolationRows}, " +
            $"BgW={metrics.WinBackgammonHierarchyViolationRows}, loseCompl={metrics.LoseGammonComplementViolationRows}, " +
            $"BgL={metrics.LoseBackgammonHierarchyViolationRows}";
    }
}