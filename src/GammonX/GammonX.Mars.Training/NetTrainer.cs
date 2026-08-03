using GammonX.Mars.NN.Nets;

using GammonX.Models.Enums;

using TorchSharp;

using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace GammonX.Mars.Training;

public static class NetTrainer
{
    public static void Train(
        GameModus modus,
        string trainCsvPath,
        string valCsvPath,
        string outputModelPath,
        int epochs = 200,
        int batchSize = 4096,
        float learningRate = 1.5e-3f,
        int earlyStoppingPatience = 25,
        bool shuffleLabels = false)
    {
        // TODO: enable full GAME equity predictions for plakoto/fevga
        var labelCount = (modus == GameModus.Fevga || modus == GameModus.Plakoto) ? 1 : 5;

        var (trainOffsets, trainRowCount, featureCols, _) = CsvBatchEnumerator.BuildRowIndex(trainCsvPath, labelCount);
        var (valOffsets, valRowCount, _, _) = CsvBatchEnumerator.BuildRowIndex(valCsvPath, labelCount);

        var device = cuda.is_available() ? CUDA : CPU;
        Console.WriteLine($"Device: {device}");

        int[]? labelPermutation = null;
        if (shuffleLabels)
        {
            Console.WriteLine("WARNING: label shuffling enabled, not a real training run.");
            labelPermutation = Enumerable.Range(0, trainRowCount).OrderBy(_ => Random.Shared.Next()).ToArray();
        }

        var model = NetModelFactory.Create(modus, device);
        model.MoveTo(device);

        var optimizer = optim.Adam(model.GetParameters(), lr: learningRate, weight_decay: 5e-4);
        var scheduler = optim.lr_scheduler.StepLR(optimizer, step_size: 20, gamma: 0.66);
        var loss = BCELoss();

        Console.WriteLine($"Train={trainRowCount}  Val={valRowCount}");
        // TODO: reactivate
        // PrintLabelStats(trainCsvPath, trainOffsets, featureCols, "train");
        // PrintLabelStats(valCsvPath, valOffsets, featureCols, "val");

        var valOrder = Enumerable.Range(0, valRowCount).ToArray();
        int[]? valLabelPerm = shuffleLabels
            ? Enumerable.Range(0, valRowCount).OrderBy(_ => Random.Shared.Next()).ToArray()
            : null;

        var bestValLoss = float.MaxValue;
        var epochsWithoutImprovement = 0;
        var bestEpoch = 0;

        for (var epoch = 1; epoch <= epochs; epoch++)
        {
            // We shuffle training row order each epoch for better convergence
            var trainOrder = Enumerable.Range(0, trainRowCount).OrderBy(_ => Random.Shared.Next()).ToArray();

            var trainBatches = new CsvBatchEnumerator(trainCsvPath, batchSize, labelCount, featureCols, trainOffsets, trainOrder, device, labelPermutation);
            var valBatches = new CsvBatchEnumerator(valCsvPath, batchSize, labelCount, featureCols, valOffsets, valOrder, device, valLabelPerm);

            model.Train();
            var trainLoss = RunEpochStreaming(model, optimizer, loss, trainBatches, true);

            model.Eval();
            float valLoss;
            using (no_grad())
                valLoss = RunEpochStreaming(model, optimizer, loss, valBatches, false);

            var currentLr = optimizer.ParamGroups.First().LearningRate;

            if (valLoss < bestValLoss)
            {
                bestValLoss = valLoss;
                bestEpoch = epoch;
                epochsWithoutImprovement = 0;
                model.Save(outputModelPath);
            }
            else
            {
                epochsWithoutImprovement++;
            }

            var marker = epoch == bestEpoch ? " OK" : "";
            Console.WriteLine($"Epoch {epoch,3}/{epochs}  train_loss={trainLoss:F5}  val_loss={valLoss:F5}  lr={currentLr:G3}{marker}");

            if (epochsWithoutImprovement >= earlyStoppingPatience)
            {
                Console.WriteLine($"Early stopping — best val_loss={bestValLoss:F5} at epoch {bestEpoch}");
                break;
            }

            scheduler.step();
        }

        Console.WriteLine($"Model saved: {outputModelPath}  (epoch {bestEpoch}  val_loss: {bestValLoss:F5})");
    }
        
    private static float RunEpochStreaming(
        INetModel model,
        optim.Optimizer optimizer,
        Loss<Tensor, Tensor, Tensor> loss,
        IEnumerable<(Tensor features, Tensor labels)> batches,
        bool train)
    {
        var totalLoss = 0f;
        var batchCount = 0;

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

                totalLoss += l.item<float>();
                batchCount++;
            }
        }

        return totalLoss / batchCount;
    }

    private static void PrintLabelStats(string csvPath, long[] offsets, int featureCols, string name)
    {
        var count = 0;
        var sum = 0.0;
        var min = float.MaxValue;
        var max = float.MinValue;
        var near05Count = 0;

        using var stream = new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16);
        using var reader = new StreamReader(stream);

        for (var i = 0; i < offsets.Length; i++)
        {
            stream.Seek(offsets[i], SeekOrigin.Begin);
            reader.DiscardBufferedData();
            var line = reader.ReadLine();
            if (line == null) 
                continue;

            // We skip feature columns, read first label (pWin)
            var span = line.AsSpan();
            var colIndex = 0;
            while (!span.IsEmpty && colIndex < featureCols)
            {
                var commaPos = span.IndexOf(',');
                colIndex++;
                span = commaPos >= 0 ? span[(commaPos + 1)..] : [];
            }

            var labelComma = span.IndexOf(',');
            var field = labelComma >= 0 ? span[..labelComma] : span;
            var value = float.Parse(field, System.Globalization.CultureInfo.InvariantCulture);

            sum += value;
            if (value < min) 
                min = value;
            if (value > max)
                max = value;
            if (Math.Abs(value - 0.5f) < 0.05f) 
                near05Count++;
            count++;
        }

        var mean = sum / count;
        var near05 = near05Count / (float)count;
        Console.WriteLine($"[{name}] n={count}  mean={mean:F4}  min={min:F4}  max={max:F4}  near-0.5={near05:P1}");
    }
}