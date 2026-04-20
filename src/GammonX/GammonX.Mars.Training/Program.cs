using GammonX.Mars.Server.Services;

using GammonX.Mars.Training;

using GammonX.Models.Enums;

// -- Configuration ------------------------------------------------------------
const int totalGames = 1_000;
const GameModus modus = GameModus.Plakoto;
const string outputPath = "training_data.csv";
const float lambda = SelfPlayRecorder.DefaultLambda;
// -----------------------------------------------------------------------------

IFeatureVectorExtractor extractor = modus switch
{
    GameModus.Plakoto => new PlakotoFeatureVectorExtractor(),
    GameModus.Fevga => new FevgaFeatureVectorExtractor(),
    _ => throw new NotSupportedException($"Modus {modus} has no feature extractor.")
};

var completed = 0;
var discarded = 0;

Console.WriteLine($"Starting self-play: {totalGames} games, modus={modus}, lambda={lambda}");
Console.WriteLine($"Output: {Path.GetFullPath(outputPath)}");
Console.WriteLine();

var allSamples = new List<(float[] Features, float Label)>(capacity: totalGames * 40);
var lockObj = new object();

Parallel.For(0, totalGames, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, _ =>
{
    var recorder = new SelfPlayRecorder(extractor, lambda);
    var runner = new SelfPlayRunner(recorder, modus);
    var samples = runner.Run();

    lock (lockObj)
    {
        if (samples.Count == 0)
        {
            discarded++;
        }
        else
        {
            allSamples.AddRange(samples);
            completed++;
        }

        var started = completed + discarded;
        Console.WriteLine($"  {started,6} / {totalGames} completed={completed} discarded={discarded} samples={allSamples.Count:N0}");
    }
});

Console.WriteLine();
Console.WriteLine($"Done. Completed={completed}  Discarded={discarded}  Total samples={allSamples.Count:N0}");
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
Console.WriteLine("Complete.");

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
