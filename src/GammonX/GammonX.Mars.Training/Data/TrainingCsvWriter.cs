using System.Globalization;

using GammonX.Mars.Training.Sidecars;
using GammonX.Models.Enums;

namespace GammonX.Mars.Training.Data;

public static class TrainingCsvWriter
{
    public static void Write(
        string path,
        GameModus modus,
        IReadOnlyList<TrainingDataRow> samples,
        int featureCount)
    {
        using var writer = new StreamWriter(path);

        var labelCount = WellKnownCsvHeaders.LabelCount(modus);
        var labelHeaders = WellKnownCsvHeaders.TrainingHeader(labelCount);

        writer.WriteLine(string.Join(",", Enumerable.Range(0, featureCount).Select(index => $"f{index}")) + "," + labelHeaders);

        foreach (var sample in samples)
        {
            writer.Write(string.Join(",", sample.Position.Features.Select(FormatFloat)));
            writer.Write(',');

            if (sample.Position.Features.Length != featureCount || sample.Label.Length != 5)
            {
                throw new InvalidDataException("Training rows must contain the expected features and five canonical outcome labels.");
            }

            float[] labels = labelCount == 3
                ? [sample.Label[0], sample.Label[1], sample.Label[3]]
                : sample.Label;

            writer.WriteLine(string.Join(",", labels.Select(FormatFloat)));
        }
    }

    private static string FormatFloat(float value) => value.ToString("G6", CultureInfo.InvariantCulture);
}