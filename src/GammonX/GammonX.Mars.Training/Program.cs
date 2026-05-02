using GammonX.Mars.Training;

using GammonX.Models.Enums;

using GammonX.Mars.NN;
using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;

using System.Diagnostics;

Console.WriteLine("===========================================");
Console.WriteLine("  GammonX Mars — Training Console");
Console.WriteLine("===========================================");
Console.WriteLine();
Console.WriteLine("  1  Generate training data");
Console.WriteLine("  2  Train model");
Console.WriteLine();
Console.Write("Select mode: ");

var modeInput = Console.ReadLine()?.Trim();
if (modeInput == "1")
{
    RunGenerateTrainingData();
}
else if (modeInput == "2")
{
    RunTrainModel();
}
else
{
    Console.WriteLine("Invalid selection. Exiting.");
}

Console.ReadLine();

#region Train Model

static void RunTrainModel()
{
    Console.WriteLine();

    var modus = PromptEnum("Game modus", new[] { GameModus.Plakoto, GameModus.Fevga }, GameModus.Plakoto);
    var trainingCsvPath = PromptString("Training CSV path", "training_data.csv");
    var outputModelPath = PromptString("Output model path", $"training_net.dat");

    NetTrainer.Train(
        modus,
        trainCsvPath: trainingCsvPath,
        valCsvPath: Path.ChangeExtension(trainingCsvPath, ".val.csv"),
        outputModelPath: outputModelPath);
}

#endregion Train Model

#region Generate Training Data

static void RunGenerateTrainingData()
{
    Console.WriteLine();

    var modus = PromptEnum("Game modus", new[] { GameModus.Plakoto, GameModus.Fevga }, GameModus.Plakoto);
    var totalGames = PromptInt("Total games", 1_000);
    var outputPath = PromptString("Output CSV path", "training_data.csv");
    var modelPath = PromptString("Neural model path (e.g. training_net.dat). Leave blank to use hard coded weights.", "");
    var lambda = PromptFloat("TD-lambda", SelfPlayRecorder.DefaultLambda);

    Console.WriteLine();

    IFeatureVectorExtractor extractor = modus switch
    {
        GameModus.Plakoto => new PlakotoFeatureVectorExtractor(),
        GameModus.Fevga => new FevgaFeatureVectorExtractor(),
        _ => throw new NotSupportedException($"Modus {modus} has no feature extractor.")
    };
    ContactWeightModel contactWeights = modus switch
    {
        GameModus.Plakoto => EvalWeights.PlakotoContactWeights,
        GameModus.Fevga => EvalWeights.FevgaContactWeights,
        _ => throw new NotSupportedException($"Modus {modus} has no contact weights.")
    };
    ContactWeightModel cheapContactWeights = modus switch
    {
        GameModus.Plakoto => EvalWeights.PlakotoCheapContactWeights,
        GameModus.Fevga => EvalWeights.FevgaCheapContactWeights,
        _ => throw new NotSupportedException($"Modus {modus} has no cheap contact weights.")
    };
    RaceWeightModel raceWeightModel = modus switch
    {
        GameModus.Plakoto => EvalWeights.RaceWeights,
        GameModus.Fevga => EvalWeights.RaceWeights,
        _ => throw new NotSupportedException($"Modus {modus} has no race weights.")
    };

    var useNeuralEval = !string.IsNullOrEmpty(modelPath) && File.Exists(modelPath);
    INeuralEvalService neuralEvalService = null!;
    if (useNeuralEval)
    {
        neuralEvalService = NeuralEvalService.Load(modus, modelPath);
        Console.WriteLine($"Loaded neural evaluator: {modelPath}");
    }
    else
    {
        Console.WriteLine("Using hand-crafted eval weights.");
    }

    var completed = 0;
    var discarded = 0;
    var allSamples = new List<(float[] Features, float Label)>(capacity: totalGames * 40);
    var totalTurnCount = 0L;
    var totalPredVariance = 0.0;
    var predVarianceCount = 0;
    var lockObj = new object();

    Console.WriteLine($"Starting self-play: {totalGames} games, modus={modus}, lambda={lambda}");
    Console.WriteLine($"Output: {Path.GetFullPath(outputPath)}");
    Console.WriteLine();

    var stopwatch = Stopwatch.StartNew();

    Parallel.For(
        0, 
        totalGames,
        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
        (_) =>
        {
            var recorder = new SelfPlayRecorder(extractor, neuralEvalService, lambda);
            var runner = new SelfPlayRunner(recorder, modus, neuralEvalService);
            var result = runner.Run(contactWeights, cheapContactWeights, raceWeightModel);

            lock (lockObj)
            {
                totalTurnCount += result.TurnCount;

                if (result.Samples.Count == 0)
                {
                    discarded++;
                }
                else
                {
                    allSamples.AddRange(result.Samples);
                    completed++;

                    if (result.PredictionVariance.HasValue)
                    {
                        totalPredVariance += result.PredictionVariance.Value;
                        predVarianceCount++;
                    }
                }

                var started = completed + discarded;
                Console.WriteLine($"  {started,6} / {totalGames} completed={completed} discarded={discarded} samples={allSamples.Count:N0}");
            }
        });

    Console.WriteLine();
    Console.WriteLine($"Done. Completed={completed}  Discarded={discarded}  Total samples={allSamples.Count:N0}");
    Console.WriteLine($"Avg turns/game : {(double)totalTurnCount / totalGames:F1}");
    if (predVarianceCount > 0)
        Console.WriteLine($"Avg pred variance: {totalPredVariance / predVarianceCount:F5}  (over {predVarianceCount} completed games)");
    Console.WriteLine("Shuffling...");

    var rng = Random.Shared;
    for (int i = allSamples.Count - 1; i > 0; i--)
    {
        int j = rng.Next(i + 1);
        (allSamples[i], allSamples[j]) = (allSamples[j], allSamples[i]);
    }

    int splitIndex = (int)(allSamples.Count * 0.85);
    var trainSamples = allSamples[..splitIndex];
    var valSamples = allSamples[splitIndex..];

    Console.WriteLine($"Train={trainSamples.Count:N0}  Validation={valSamples.Count:N0}");
    Console.WriteLine("Writing CSV files...");

    WriteCsv(outputPath, trainSamples, extractor.FeatureCount);
    WriteCsv(Path.ChangeExtension(outputPath, ".val.csv"), valSamples, extractor.FeatureCount);

    Console.WriteLine($"Written: {outputPath}");
    Console.WriteLine($"Written: {Path.ChangeExtension(outputPath, ".val.csv")}");
    Console.WriteLine($"Elapsed: {stopwatch.Elapsed:dd\\:hh\\:mm\\:ss}");
    Console.WriteLine("Complete.");
}

static void WriteCsv(string path, List<(float[] Features, float Label)> samples, int featureCount)
{
    using var writer = new StreamWriter(path);
    writer.WriteLine(string.Join(",", Enumerable.Range(0, featureCount).Select(i => $"f{i}")) + ",label");
    foreach (var (features, label) in samples)
    {
        writer.Write(string.Join(",", features.Select(f => f.ToString("G6"))));
        writer.Write(',');
        writer.WriteLine(label.ToString("G6"));
    }
}

#endregion Generate Training Data

#region Prompt Helpers

static GameModus PromptEnum(string label, GameModus[] options, GameModus defaultValue)
{
    Console.WriteLine($"{label}:");
    for (int i = 0; i < options.Length; i++)
        Console.WriteLine($"  {i + 1}  {options[i]}");
    Console.Write($"Select [{defaultValue}]: ");
    var input = Console.ReadLine()?.Trim();
    if (int.TryParse(input, out var idx) && idx >= 1 && idx <= options.Length)
        return options[idx - 1];
    return defaultValue;
}

static int PromptInt(string label, int defaultValue)
{
    Console.Write($"{label} [{defaultValue}]: ");
    var input = Console.ReadLine()?.Trim();
    return int.TryParse(input, out var v) ? v : defaultValue;
}

static float PromptFloat(string label, float defaultValue)
{
    Console.Write($"{label} [{defaultValue}]: ");
    var input = Console.ReadLine()?.Trim();
    return float.TryParse(input, out var v) ? v : defaultValue;
}

static string PromptString(string label, string defaultValue)
{
    Console.Write($"{label} [{defaultValue}]: ");
    var input = Console.ReadLine()?.Trim();
    return string.IsNullOrEmpty(input) ? defaultValue : input;
}

#endregion Prompt Helpers