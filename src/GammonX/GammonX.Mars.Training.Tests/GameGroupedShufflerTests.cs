using GammonX.Mars.Training.Data;

namespace GammonX.Mars.Training.Tests;

public sealed class GameGroupedShufflerTests
{
    [Fact]
    public void SplitKeepsEachGameInOnePartitionAndPreservesAllRows()
    {
        var gameA = Guid.NewGuid();
        var gameB = Guid.NewGuid();
        var gameC = Guid.NewGuid();
        var rows = new[]
        {
            new SampleRow(gameA, 0),
            new SampleRow(gameA, 1),
            new SampleRow(gameB, 0),
            new SampleRow(gameC, 0),
            new SampleRow(gameB, 1),
            new SampleRow(gameC, 1),
            new SampleRow(gameC, 2)
        };

        var split = GameGroupedShuffler.SplitByGame(rows, row => row.GameId, 0.6, new Random(17));
        var trainingGames = split.Training.Select(row => row.GameId).ToHashSet();
        var validationGames = split.Validation.Select(row => row.GameId).ToHashSet();
        var combined = split.Training.Concat(split.Validation).ToArray();

        Assert.Empty(trainingGames.Intersect(validationGames));
        Assert.Equal(rows.Length, combined.Length);
        Assert.Equal(
            rows.OrderBy(row => row.GameId).ThenBy(row => row.Turn),
            combined.OrderBy(row => row.GameId).ThenBy(row => row.Turn));
        Assert.Equal(
            rows.Select(row => row.GameId).Distinct().OrderBy(gameId => gameId),
            trainingGames.Concat(validationGames).OrderBy(gameId => gameId));
    }

    [Fact]
    public void SplitGroupsNonContiguousRowsAndPreservesTheirInputOrder()
    {
        var gameA = Guid.NewGuid();
        var gameB = Guid.NewGuid();
        var rows = new[]
        {
            new SampleRow(gameA, 0),
            new SampleRow(gameB, 0),
            new SampleRow(gameA, 1),
            new SampleRow(gameB, 1),
            new SampleRow(gameA, 2)
        };

        var split = GameGroupedShuffler.SplitByGame(rows, row => row.GameId, 0.5, new Random(3));
        var combined = split.Training.Concat(split.Validation).ToArray();

        foreach (var gameRows in rows.GroupBy(row => row.GameId))
        {
            var actualTurns = combined
                .Where(row => row.GameId == gameRows.Key)
                .Select(row => row.Turn);
            Assert.Equal(gameRows.Select(row => row.Turn), actualTurns);
        }

        var seenGames = new HashSet<Guid>();
        Guid? previousGame = null;
        foreach (var row in combined)
        {
            if (previousGame != row.GameId)
                Assert.True(seenGames.Add(row.GameId));

            previousGame = row.GameId;
        }
    }

    [Fact]
    public void SplitUsesWholeGamesAndStaysCloseToRequestedRowFraction()
    {
        var rows = Enumerable.Range(0, 10)
            .SelectMany(game => Enumerable.Range(0, 10).Select(turn => new SampleRow(GuidFromInt(game), turn)))
            .ToArray();

        var split = GameGroupedShuffler.SplitByGame(rows, row => row.GameId, 0.85, new Random(23));
        var actualFraction = (double)split.Training.Count / rows.Length;

        Assert.InRange(actualFraction, 0.8, 0.9);
        Assert.Equal(0, split.Training.Count % 10);
        Assert.Equal(0, split.Validation.Count % 10);
    }

    [Fact]
    public void SplitIsRepeatableWithTheSameRandomSeed()
    {
        var rows = Enumerable.Range(0, 8)
            .SelectMany(game => Enumerable.Range(0, game + 1).Select(turn => new SampleRow(GuidFromInt(game), turn)))
            .ToArray();

        var first = GameGroupedShuffler.SplitByGame(rows, row => row.GameId, 0.75, new Random(31));
        var second = GameGroupedShuffler.SplitByGame(rows, row => row.GameId, 0.75, new Random(31));

        Assert.Equal(first.Training, second.Training);
        Assert.Equal(first.Validation, second.Validation);
    }

    [Fact]
    public void EmptyInputProducesTwoEmptyPartitions()
    {
        var split = GameGroupedShuffler.SplitByGame(
            Array.Empty<SampleRow>(),
            row => row.GameId,
            0.85,
            new Random(7));

        Assert.Empty(split.Training);
        Assert.Empty(split.Validation);
    }

    [Fact]
    public void SingleGameIsKeptTogether()
    {
        var gameId = Guid.NewGuid();
        var rows = Enumerable.Range(0, 3)
            .Select(turn => new SampleRow(gameId, turn))
            .ToArray();

        var split = GameGroupedShuffler.SplitByGame(rows, row => row.GameId, 0.85, new Random(11));

        Assert.Equal(rows, split.Training);
        Assert.Empty(split.Validation);
    }

    [Fact]
    public void SelectGamesReturnsDistinctRequestedCount()
    {
        var gameIds = new[]
        {
            GuidFromInt(1),
            GuidFromInt(2),
            GuidFromInt(1),
            GuidFromInt(3),
            GuidFromInt(4)
        };

        var selected = GameGroupedShuffler.SelectGames(gameIds, 3, new Random(17));

        Assert.Equal(3, selected.Count);
        Assert.Equal(3, selected.Distinct().Count());
        Assert.All(selected, gameId => Assert.Contains(gameId, gameIds));
    }

    [Fact]
    public void SelectGamesIsRepeatableWithTheSameRandomSeed()
    {
        var gameIds = Enumerable.Range(0, 8).Select(GuidFromInt).ToArray();

        var first = GameGroupedShuffler.SelectGames(gameIds, 4, new Random(31));
        var second = GameGroupedShuffler.SelectGames(gameIds, 4, new Random(31));

        Assert.Equal(first, second);
    }

    [Fact]
    public void SelectGamesRejectsCountOutsideAvailableRange()
    {
        var gameIds = new[] { GuidFromInt(1), GuidFromInt(2) };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => GameGroupedShuffler.SelectGames(gameIds, -1, new Random(1)));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GameGroupedShuffler.SelectGames(gameIds, 3, new Random(1)));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1d)]
    [InlineData(-0.1d)]
    [InlineData(1.1d)]
    public void InvalidTrainFractionIsRejected(double trainFraction)
    {
        var rows = new[] { new SampleRow(Guid.NewGuid(), 0) };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => GameGroupedShuffler.SplitByGame(rows, row => row.GameId, trainFraction, new Random(1)));
    }

    private static Guid GuidFromInt(int value)
        => new(value, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    private sealed record SampleRow(Guid GameId, int Turn);
}