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

        // TODO: implement 5 head ouput for fevga and plakoto
        var singleHead = modus is GameModus.Fevga or GameModus.Plakoto;
        var labelHeaders = singleHead
            ? "pWin"
            : "pWin,pGammonWin,pBackgammonWin,pGammonLoss,pBackgammonLoss";

        writer.WriteLine(string.Join(",", Enumerable.Range(0, featureCount).Select(index => $"f{index}")) + "," + labelHeaders);

        foreach (var sample in samples)
        {
            writer.Write(string.Join(",", sample.Position.Features.Select(FormatFloat)));
            writer.Write(',');
            writer.WriteLine(singleHead
                ? FormatFloat(sample.Label[0])
                : string.Join(",", sample.Label.Select(FormatFloat)));
        }
    }

    private static string FormatFloat(float value)
        => value.ToString("G6", CultureInfo.InvariantCulture);
}