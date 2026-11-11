using System.Globalization;

using GammonX.Mars.Training.Data;
using GammonX.Mars.Training.Sidecars;

using GammonX.Models.Enums;

namespace GammonX.Mars.Training.Tests;

public sealed class TrainingCsvWriterTests
{
    [Theory]
    [InlineData("f0,f1,pWin")]
    [InlineData("f0,f1,pWin,pGammonLoss,pGammonWin")]
    [InlineData("f0,f1,pWin,pGammonWin,pBackgammonWin,pGammonLoss,pBackgammonLoss")]
    public void ThreeLabelReadersRejectIncompatibleHeaders(string header)
    {
        var path = Path.Combine(Path.GetTempPath(), $"gammonx-header-{Guid.NewGuid():N}.csv");
        var binary = Path.ChangeExtension(path, ".bin");
        try
        {
            File.WriteAllText(path, header + "\n");
            Assert.Throws<InvalidDataException>(() => CsvBatchEnumerator.BuildRowIndex(path, 3));
            Assert.Throws<InvalidDataException>(() => BinaryBatchEnumerator.ScanCsvAndConvertToBinary(path, binary, 3));
        }
        finally
        {
            File.Delete(path);
            File.Delete(binary);
        }
    }

    [Theory]
    [InlineData(GameModus.Fevga, 3)]
    [InlineData(GameModus.Plakoto, 3)]
    [InlineData(GameModus.Backgammon, 5)]
    public void WriteUsesInvariantCultureAndProducesReadableCsv(GameModus modus, int labelCount)
    {
        var csvPath = Path.Combine(Path.GetTempPath(), $"gammonx-training-{Guid.NewGuid():N}.csv");
        var binaryPath = Path.ChangeExtension(csvPath, ".bin");
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            var labels = new[] { 0.25f, 0.125f, 0f, 0.1f, 0f };
            var row = new TrainingDataRow(
                Guid.NewGuid(),
                new TrajectoryPosition(0, true, [1.5f, -2.25f], [0.5f, 0f, 0f, 0f, 0f], true),
                labels);

            TrainingCsvWriter.Write(csvPath, modus, [row], featureCount: 2);

            var lines = File.ReadAllLines(csvPath);
            Assert.Equal(2 + labelCount, lines[1].Split(',').Length);
            Assert.Contains("1.5", lines[1]);
            Assert.Contains("0.25", lines[1]);
            if (labelCount == 3)
            {
                Assert.EndsWith(",pWin,pGammonWin,pGammonLoss", lines[0]);
                Assert.EndsWith(",0.25,0.125,0.1", lines[1]);
            }

            var converted = BinaryBatchEnumerator.ScanCsvAndConvertToBinary(csvPath, binaryPath, labelCount);
            Assert.Equal(2, converted.featureCount);
            Assert.Equal(1, converted.rowCount);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
            if (File.Exists(csvPath))
                File.Delete(csvPath);
            if (File.Exists(binaryPath))
                File.Delete(binaryPath);
        }
    }
}