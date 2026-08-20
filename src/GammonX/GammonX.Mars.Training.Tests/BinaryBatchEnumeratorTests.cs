using GammonX.Mars.Training.Data;
using static TorchSharp.torch;

namespace GammonX.Mars.Training.Tests;

public sealed class BinaryBatchEnumeratorTests
{
    [Fact]
    public void Enumerator_ThrowsWhenBinaryRowIsTruncated()
    {
        var binaryPath = CreateTempPath(".bin");
        try
        {
            File.WriteAllBytes(binaryPath, new byte[3]);

            var enumerator = new BinaryBatchEnumerator(
                binaryPath,
                batchSize: 1,
                labelCount: 1,
                featureCount: 1,
                rowOrder: [0],
                device: CPU);

            var exception = Assert.Throws<AggregateException>(() => enumerator.ToList());
            var dataException = Assert.IsType<InvalidDataException>(Assert.Single(exception.InnerExceptions));
            Assert.Contains("Binary row 0 is truncated", dataException.Message);
        }
        finally
        {
            DeleteIfExists(binaryPath);
        }
    }

    [Fact]
    public void Enumerator_UsesPermutationForLabelsOnly()
    {
        var csvPath = CreateTempPath(".csv");
        var binaryPath = CreateTempPath(".bin");
        try
        {
            File.WriteAllText(
                csvPath,
                "f0,f1,pWin\n" +
                "1,10,0.1\n" +
                "2,20,0.2\n" +
                "3,30,0.3\n");

            var (featureCount, rowCount, _) = BinaryBatchEnumerator.ScanCsvAndConvertToBinary(csvPath, binaryPath, labelCount: 1);
            var enumerator = new BinaryBatchEnumerator(
                binaryPath,
                batchSize: rowCount,
                labelCount: 1,
                featureCount,
                rowOrder: [0, 1, 2],
                device: CPU,
                labelPermutation: [2, 0, 1]);

            var (features, labels) = Assert.Single(enumerator);
            using (features)
            using (labels)
            {
                Assert.Equal([1f, 10f, 2f, 20f, 3f, 30f], features.data<float>().ToArray());
                Assert.Equal([0.3f, 0.1f, 0.2f], labels.data<float>().ToArray());
            }
        }
        finally
        {
            DeleteIfExists(csvPath);
            DeleteIfExists(binaryPath);
        }
    }

    [Fact]
    public void SidecarFileDoesNotChangeOrdinaryTrainingCsvLayout()
    {
        var csvPath = CreateTempPath(".csv");
        var binaryPath = CreateTempPath(".bin");
        var sidecarPath = Path.ChangeExtension(csvPath, ".trajectory.csv");
        try
        {
            File.WriteAllText(csvPath, "f0,f1,pWin\n1,10,0.25\n");
            File.WriteAllText(sidecarPath, "gameId,turnIndex,isWhite,isTerminal,pWin,pGammonWin,pBackgammonWin,pGammonLoss,pBackgammonLoss\n" +
                $"{Guid.NewGuid():D},0,1,1,0.5,0,0,0,0\n");

            var (featureCount, rowCount, _) = BinaryBatchEnumerator.ScanCsvAndConvertToBinary(csvPath, binaryPath, labelCount: 1);

            Assert.Equal(2, featureCount);
            Assert.Equal(1, rowCount);
        }
        finally
        {
            DeleteIfExists(csvPath);
            DeleteIfExists(binaryPath);
            DeleteIfExists(sidecarPath);
        }
    }

    [Theory]
    [InlineData("1,2")]
    [InlineData("1,2,0.5,extra")]
    public void ScanCsvAndConvertToBinary_RejectsUnexpectedColumnCount(string row)
    {
        var csvPath = CreateTempPath(".csv");
        var binaryPath = CreateTempPath(".bin");
        try
        {
            File.WriteAllText(csvPath, $"f0,f1,pWin\n{row}\n");

            var exception = Assert.Throws<InvalidDataException>(
                () => BinaryBatchEnumerator.ScanCsvAndConvertToBinary(csvPath, binaryPath, labelCount: 1));

            Assert.Contains("columns", exception.Message);
            Assert.Contains("expected 3", exception.Message);
        }
        finally
        {
            DeleteIfExists(csvPath);
            DeleteIfExists(binaryPath);
        }
    }

    private static string CreateTempPath(string extension)
        => Path.Combine(Path.GetTempPath(), $"gammonx-training-{Guid.NewGuid():N}{extension}");

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
