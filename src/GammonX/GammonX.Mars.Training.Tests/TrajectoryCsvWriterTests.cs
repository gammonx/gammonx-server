using System.Globalization;
using GammonX.Mars.Training.Sidecars;
using GammonX.Models.Enums;

namespace GammonX.Mars.Training.Tests;

public sealed class TrajectoryCsvWriterTests
{
    [Fact]
    public void PositionSidecarWritesTrajectoryMetadataAndPredictions()
    {
        var path = CreateTempPath(".trajectory.csv");
        try
        {
            var gameId = Guid.NewGuid();
            var position = new TrajectoryPosition(
                2,
                false,
                [0.1f, 0.2f],
                [0.6f, 0.2f, 0.1f, 0.15f, 0.05f],
                true);
            var row = new TrainingDataRow(gameId, position, [1f, 1f, 0f, 0f, 0f]);

            TrajectoryCsvWriter.WritePositions(path, [row]);

            var lines = File.ReadAllLines(path);
            Assert.Equal("gameId,turnIndex,isWhite,isTerminal,pWin,pGammonWin,pBackgammonWin,pGammonLoss,pBackgammonLoss", lines[0]);
            var columns = lines[1].Split(',');
            Assert.Equal(gameId.ToString("D"), columns[0]);
            Assert.Equal("2", columns[1]);
            Assert.Equal("0", columns[2]);
            Assert.Equal("1", columns[3]);
            Assert.Equal(0.6f, float.Parse(columns[4], CultureInfo.InvariantCulture));
            Assert.Equal(0.2f, float.Parse(columns[5], CultureInfo.InvariantCulture));
            Assert.Equal(0.1f, float.Parse(columns[6], CultureInfo.InvariantCulture));
            Assert.Equal(0.15f, float.Parse(columns[7], CultureInfo.InvariantCulture));
            Assert.Equal(0.05f, float.Parse(columns[8], CultureInfo.InvariantCulture));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public void GameSidecarWritesTerminalMetadata()
    {
        var path = CreateTempPath(".games.csv");
        try
        {
            var gameId = Guid.NewGuid();
            TrajectoryCsvWriter.WriteGames(
                path,
                [new GameMetadata(gameId, 12, true, GameResult.Gammon, GameResult.LostGammon)]);

            var lines = File.ReadAllLines(path);
            Assert.Equal("gameId,totalTurns,whiteWon,winnerResult,loserResult", lines[0]);
            Assert.Equal($"{gameId:D},12,1,Gammon,LostGammon", lines[1]);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    private static string CreateTempPath(string extension)
        => Path.Combine(Path.GetTempPath(), $"gammonx-trajectory-{Guid.NewGuid():N}{extension}");

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
