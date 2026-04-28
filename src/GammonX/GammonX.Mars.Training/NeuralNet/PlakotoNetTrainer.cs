using TorchSharp;

using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace GammonX.Mars.Training.NeuralNet;

public static class PlakotoNetTrainer
{
    public static void Train(
        string trainCsvPath,
        string valCsvPath,
        string outputModelPath,
        int epochs = 100,
        int batchSize = 256,
        float learningRate = 3e-3f)
    {
        var (trainFeatures, trainLabels) = LoadCsv(trainCsvPath);
        var (valFeatures, valLabels) = LoadCsv(valCsvPath);

        var model = new PlakotoNet();
        var optimizer = optim.Adam(model.parameters(), lr: learningRate);
        var scheduler = optim.lr_scheduler.StepLR(optimizer, step_size: 20, gamma: 0.5);
        var loss = BCELoss();

        Console.WriteLine($"Train={trainFeatures.shape[0]}  Val={valFeatures.shape[0]}");

        PrintLabelStats(trainCsvPath, "train");
        PrintLabelStats(valCsvPath, "val");

        for (var epoch = 1; epoch <= epochs; epoch++)
        {
            model.train();
            var trainLoss = RunEpoch(model, optimizer, loss, trainFeatures, trainLabels, batchSize, train: true);

            model.eval();
            float valLoss;
            using (no_grad())
                valLoss = RunEpoch(model, optimizer, loss, valFeatures, valLabels, batchSize, train: false);

            var currentLr = optimizer.ParamGroups.First().LearningRate;
            Console.WriteLine($"Epoch {epoch,3}/{epochs}  train_loss={trainLoss:F5}  val_loss={valLoss:F5}  lr={currentLr:G3}");

            scheduler.step();
        }

        model.save(outputModelPath);
        Console.WriteLine($"Model saved: {outputModelPath}");
    }

    private static float RunEpoch(
        PlakotoNet model,
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
            var xBatch = features.narrow(0, start, end - start);
            var yBatch = labels.narrow(0, start, end - start);

            var pred = model.forward(xBatch);
            var l = loss.forward(pred, yBatch);

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
        var lines = File.ReadAllLines(path);
        // we skip header
        var rows = lines.Length - 1;
        var firstData = lines[1].Split(',');
        // we expect last column to be a label
        var cols = firstData.Length - 1;

        var features = new float[rows * cols];
        var labels = new float[rows];

        for (var i = 0; i < rows; i++)
        {
            var parts = lines[i + 1].Split(',');
            for (var j = 0; j < cols; j++)
                features[i * cols + j] = float.Parse(parts[j]);
            labels[i] = float.Parse(parts[cols]);
        }

        return (
            tensor(features, new long[] { rows, cols }),
            tensor(labels)
        );
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