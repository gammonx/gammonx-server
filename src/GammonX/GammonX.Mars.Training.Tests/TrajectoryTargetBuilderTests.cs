using GammonX.Mars.Training.Sidecars;
using GammonX.Mars.Training.Data;
using GammonX.Mars.Training.Generator;
using GammonX.Mars.Training.Validation;

using GammonX.Models.Enums;

namespace GammonX.Mars.Training.Tests;

public sealed class TrajectoryTargetBuilderTests
{
    [Fact]
    public void ReaderAndBuilderRestoreChronologicalTargetsFromShuffledRows()
    {
        var trainingPath = CreateTempPath(".csv");
        var trajectoryPath = CreateTempPath(".trajectory.csv");
        var gamesPath = CreateTempPath(".games.csv");
        try
        {
            var gameId = Guid.NewGuid();
            var rows = new[]
            {
                CreateRow(gameId, 2, true, [0.6f, 0.3f, 0.15f, 0.05f, 0.05f], true),
                CreateRow(gameId, 0, true, [0.5f, 0.2f, 0.1f, 0.1f, 0.1f], false),
                CreateRow(gameId, 1, false, [0.4f, 0.1f, 0.05f, 0.2f, 0.1f], false)
            };

            File.WriteAllLines(
                trainingPath,
                [
                    "f0,f1,pWin,pGammonWin,pBackgammonWin,pGammonLoss,pBackgammonLoss",
                    "0.2,0.2,0,0,0,0,0",
                    "0.0,0.0,0,0,0,0,0",
                    "0.1,0.1,0,0,0,0,0"
                ]);
            TrajectoryCsvWriter.WritePositions(trajectoryPath, rows);
            TrajectoryCsvWriter.WriteGames(
                gamesPath,
                [new GameMetadata(gameId, 3, true, GameResult.Gammon, GameResult.LostGammon)]);

            var loadedRows = TrajectoryCsvReader.ReadRows(trainingPath, trajectoryPath, labelCount: 5);
            var games = TrajectoryCsvReader.ReadGames(gamesPath);
            var recalculated = TrajectoryTargetBuilder.Recalculate(loadedRows, games, lambda: 1f);

            Assert.Equal([2, 0, 1], recalculated.Select(row => row.Position.TurnIndex));
            AssertVector([1f, 1f, 0f, 0f, 0f], recalculated[0].Label);
            AssertVector([1f, 1f, 0f, 0f, 0f], recalculated[1].Label);
            AssertVector([0f, 0f, 0f, 1f, 0f], recalculated[2].Label);
        }
        finally
        {
            DeleteIfExists(trainingPath);
            DeleteIfExists(trajectoryPath);
            DeleteIfExists(gamesPath);
        }
    }

    [Theory]
    [InlineData(GameModus.Fevga, 0f)]
    [InlineData(GameModus.Fevga, 0.5f)]
    [InlineData(GameModus.Plakoto, 1f)]
    public void ThreeLabelCsvRoundTripRebuildsCanonicalGammonTargets(GameModus modus, float lambda)
    {
        var trainingPath = CreateTempPath(".csv");
        var trajectoryPath = CreateTempPath(".trajectory.csv");
        try
        {
            var gameId = Guid.NewGuid();
            TrajectoryPosition[] positions =
            [
                new(0, true, [0.1f, 0.2f], [0.5f, 0.2f, 0f, 0.1f, 0f]),
                new(1, false, [0.2f, 0.3f], [0.4f, 0.1f, 0f, 0.2f, 0f]),
                new(2, true, [0.3f, 0.4f], [1f, 1f, 0f, 0f, 0f], true)
            ];
            var trajectory = new GameTrajectory(gameId, positions, GameResult.Gammon, GameResult.LostGammon, true);
            var targets = ForwardViewTdCalculator.Calculate(trajectory, lambda);
            var rows = positions.Select((p, i) => new TrainingDataRow(gameId, p, targets[i])).ToArray();
            TrainingCsvWriter.Write(trainingPath, modus, rows, 2);
            TrajectoryCsvWriter.WritePositions(trajectoryPath, rows);
            var loaded = TrajectoryCsvReader.ReadRows(trainingPath, trajectoryPath, 3);
            var rebuilt = TrajectoryTargetBuilder.Recalculate(loaded, [trajectory.Metadata], lambda);
            for (var row = 0; row < rows.Length; row++)
            {
                AssertVector(targets[row], loaded[row].Label);
                AssertVector(targets[row], rebuilt[row].Label);
                Assert.Equal(0f, rebuilt[row].Label[2]);
                Assert.Equal(0f, rebuilt[row].Label[4]);
            }
        }
        finally
        {
            DeleteIfExists(trainingPath);
            DeleteIfExists(trajectoryPath);
        }
    }

    private static TrainingDataRow CreateRow(
        Guid gameId,
        int turnIndex,
        bool isWhite,
        float[] prediction,
        bool isTerminal)
    {
        return new TrainingDataRow(
            gameId,
            new TrajectoryPosition(turnIndex, isWhite, [0.1f, 0.2f], prediction, isTerminal),
            [0f, 0f, 0f, 0f, 0f]);
    }

    private static void AssertVector(float[] expected, IReadOnlyList<float> actual)
    {
        Assert.Equal(expected.Length, actual.Count);
        for (var index = 0; index < expected.Length; index++)
            Assert.Equal(expected[index], actual[index], precision: 5);
    }

    private static string CreateTempPath(string extension)
        => Path.Combine(Path.GetTempPath(), $"gammonx-target-{Guid.NewGuid():N}{extension}");

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
