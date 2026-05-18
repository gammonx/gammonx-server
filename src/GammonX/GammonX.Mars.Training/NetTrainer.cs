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
        int epochs = 300,
        int batchSize = 4096,
        float learningRate = 1e-3f,
        int earlyStoppingPatience = 45)
    {
        var (trainFeatures, trainLabels) = LoadCsv(trainCsvPath);
        var (valFeatures, valLabels) = LoadCsv(valCsvPath);

        var model = NetModelFactory.Create(modus);
        var optimizer = optim.Adam(model.GetParameters(), lr: learningRate, weight_decay: 5e-4);
        var scheduler = optim.lr_scheduler.StepLR(optimizer, step_size: 40, gamma: 0.5);
        var loss = BCELoss();

        Console.WriteLine($"Train={trainFeatures.shape[0]}  Val={valFeatures.shape[0]}");
        PrintLabelStats(trainCsvPath, "train");
        PrintLabelStats(valCsvPath, "val");

        var bestValLoss = float.MaxValue;
        var epochsWithoutImprovement = 0;
        var bestEpoch = 0;

        for (var epoch = 1; epoch <= epochs; epoch++)
        {
            model.Train();
            var trainLoss = RunEpoch(model, optimizer, loss, trainFeatures, trainLabels, batchSize, train: true);

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
        // we read all rows first to know exact count
        var rowList = new List<string>(capacity: 2_000_000);
        using (var reader = new StreamReader(path))
        {
            // we skip header
            _ = reader.ReadLine();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line is not null)
                    rowList.Add(line);
            }
        }

        var rows = rowList.Count;
        // last column is always a label
        var cols = rowList[0].Split(',').Length - 1;

        // we build the tensor in chunks and cat them together for large datasets 
        const int chunkSize = 500_000;
        var featureChunks = new List<Tensor>();
        var labelChunks = new List<Tensor>();

        for (var start = 0; start < rows; start += chunkSize)
        {
            var end = Math.Min(start + chunkSize, rows);
            var count = end - start;

            var featBuf = new float[count * cols];
            var lblBuf = new float[count];

            for (var i = 0; i < count; i++)
            {
                var parts = rowList[start + i].Split(',');
                for (var j = 0; j < cols; j++)
                    featBuf[i * cols + j] = float.Parse(parts[j], System.Globalization.CultureInfo.InvariantCulture);
                lblBuf[i] = float.Parse(parts[cols], System.Globalization.CultureInfo.InvariantCulture);
            }

            featureChunks.Add(tensor(featBuf, new long[] { count, cols }));
            labelChunks.Add(tensor(lblBuf));
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
