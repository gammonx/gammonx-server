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
        var (trainFeatures, trainLabels) = LoadCsv(trainCsvPath);
        var (valFeatures, valLabels) = LoadCsv(valCsvPath);

        if (shuffleLabels)
        {
            Console.WriteLine("WARNING: label shuffling enabled, not a real training run.");
            trainLabels = ShuffleLabels(trainLabels);
            valLabels = ShuffleLabels(valLabels);
        }

        var model = NetModelFactory.Create(modus);
        var optimizer = optim.Adam(model.GetParameters(), lr: learningRate, weight_decay: 5e-4);
        var scheduler = optim.lr_scheduler.StepLR(optimizer, step_size: 20, gamma: 0.75);
        var loss = BCELoss();

        Console.WriteLine($"Train={trainFeatures.shape[0]}  Val={valFeatures.shape[0]}");
        PrintLabelStats(trainCsvPath, "train");
        PrintLabelStats(valCsvPath, "val");

        var bestValLoss = float.MaxValue;
        var epochsWithoutImprovement = 0;
        var bestEpoch = 0;
            
        for (var epoch = 1; epoch <= epochs; epoch++)
        {
            // we shuffle training indices each epoch for better convergence
            using var trainIdx = randperm(trainFeatures.shape[0]);
            using var shuffledFeatures = trainFeatures.index_select(0, trainIdx);
            using var shuffledLabels = trainLabels.index_select(0, trainIdx);

            model.Train();
            var trainLoss = RunEpoch(model, optimizer, loss, shuffledFeatures, shuffledLabels, batchSize, train: true);

            model.Eval();
            float valLoss;
            using (no_grad())
                valLoss = RunEpoch(model, optimizer, loss, valFeatures, valLabels, batchSize, train: false);

            var currentLr = optimizer.ParamGroups.First().LearningRate;

            if (valLoss < bestValLoss)
            {
                bestValLoss = valLoss;
                bestEpoch = epoch;
                epochsWithoutImprovement = 0;
                // save only the best
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
        
    private static float RunEpoch(
        INetModel model,
        optim.Optimizer optimizer,
        Loss<Tensor, Tensor, Tensor> loss,
        Tensor features,
        Tensor labels,
        int batchSize,
        bool train)
    {
        var n = features.shape[0];
        var totalLoss = 0f;
        var batches = 0;

        for (var start = 0; start < n; start += batchSize)
        {
            var end = Math.Min(start + batchSize, n);
            using var xBatch = features.narrow(0, start, end - start);
            using var yBatch = labels.narrow(0, start, end - start);

            using var pred = model.Forward(xBatch);
            using var l = loss.forward(pred, yBatch);

            if (train)
            {
                optimizer.zero_grad();
                l.backward();
                optimizer.step();
            }

            totalLoss += l.item<float>();
            batches++;
        }

        return totalLoss / batches;
    }

    private static (Tensor features, Tensor labels) LoadCsv(string path)
    {
        // we stream the CSV and fill tensors in chunks to avoid holding all raw strings in memory
        int cols;
        using (var headerReader = new StreamReader(path))
        {
            var header = headerReader.ReadLine()!;
            // last column is always a label
            cols = header.Split(',').Length - 1;
        }

        const int chunkSize = 500_000;
        var featureChunks = new List<Tensor>();
        var labelChunks = new List<Tensor>();

        using (var reader = new StreamReader(path))
        {
            // we skip header
            _ = reader.ReadLine();

            while (!reader.EndOfStream)
            {
                var featBuf = new float[chunkSize * cols];
                var lblBuf = new float[chunkSize];
                var count = 0;

                while (count < chunkSize && !reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                        break;

                    var span = line.AsSpan();
                    var colIndex = 0;
                    var expectedCols = cols + 1;

                    while (!span.IsEmpty && colIndex < expectedCols)
                    {
                        var commaPos = span.IndexOf(',');
                        var field = commaPos >= 0 ? span[..commaPos] : span;

                        var value = float.Parse(field, System.Globalization.CultureInfo.InvariantCulture);

                        if (colIndex < cols)
                            featBuf[count * cols + colIndex] = value;
                        else
                            lblBuf[count] = value;

                        colIndex++;
                        span = commaPos >= 0 ? span[(commaPos + 1)..] : [];
                    }

                    if (colIndex != expectedCols)
                    {
                        Console.WriteLine($"Skipping row: expected {expectedCols} columns, got {colIndex}");
                        continue;
                    }

                    count++;
                }

                if (count == 0)
                    break;

                featureChunks.Add(tensor(featBuf.AsSpan(0, count * cols).ToArray(), new long[] { count, cols }));
                labelChunks.Add(tensor(lblBuf.AsSpan(0, count).ToArray()));
            }
        }

        var featureTensor = featureChunks.Count == 1
            ? featureChunks[0]
            : cat(featureChunks, dim: 0);

        var labelTensor = labelChunks.Count == 1
            ? labelChunks[0]
            : cat(labelChunks, dim: 0);

        foreach (var t in featureChunks)
        {
            if (!ReferenceEquals(t, featureTensor))
            {
                t.Dispose();
            }
        }

        foreach (var t in labelChunks)
        {
            if (!ReferenceEquals(t, labelTensor))
            {
                t.Dispose();
            }
        }

        return (featureTensor, labelTensor);
    }

    private static Tensor ShuffleLabels(Tensor labels)
    {
        using var idx = randperm(labels.shape[0]);
        return labels.index_select(0, idx);
    }

    private static void PrintLabelStats(string path, string name)
    {
        var lines = File.ReadAllLines(path);
        var labels = lines.Skip(1)
                          .Select(l => float.Parse(l.Split(',')[^1]))
                          .ToArray();

        var mean = labels.Average();
        var min = labels.Min();
        var max = labels.Max();
        var near05 = labels.Count(l => Math.Abs(l - 0.5f) < 0.05f) / (float)labels.Length;

        Console.WriteLine($"[{name}] n={labels.Length}  mean={mean:F4}  min={min:F4}  max={max:F4}  near-0.5={near05:P1}");
    }
}
