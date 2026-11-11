using GammonX.Engine.Services;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Mars.Training.Tournament;

namespace GammonX.Mars.Training.Tests;

public sealed class TournamentRunnerTests
{
    [Fact]
    public async Task ParallelColorAssignmentsRemainIterationLocal()
    {
        var modelA = new TournamentEntry("model-a.dat", BotLevel.Hard, null, null);
        var modelB = new TournamentEntry("model-b.dat", BotLevel.Hard, null, null);
        var assignments = new (bool ModelAIsWhite, bool ModelBIsWhite)[1_000];

        await Parallel.ForAsync(0, assignments.Length, (index, _) =>
        {
            var assigned = TournamentRunner.AssignColors(modelA, modelB, index % 2 == 0);
            assignments[index] = (
                assigned.ModelA.IsWhite!.Value,
                assigned.ModelB!.IsWhite!.Value);
            return ValueTask.CompletedTask;
        });

        Assert.Null(modelA.IsWhite);
        Assert.Null(modelB.IsWhite);
        for (var index = 0; index < assignments.Length; index++)
        {
            Assert.Equal(index % 2 == 0, assignments[index].ModelAIsWhite);
            Assert.Equal(index % 2 != 0, assignments[index].ModelBIsWhite);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AssignBotPlayersMapsModelAndWildBgToRequestedColor(bool modelIsWhite)
    {
        var player1Id = Guid.NewGuid();
        var player2Id = Guid.NewGuid();

        var assignment = TournamentRunner.AssignBotPlayers(player1Id, player2Id, modelIsWhite);

        Assert.Equal(modelIsWhite ? player1Id : player2Id, assignment.ModelPlayerId);
        Assert.Equal(modelIsWhite ? player2Id : player1Id, assignment.WildBgPlayerId);
    }

    [Theory]
    [InlineData(GameResult.Single, 1)]
    [InlineData(GameResult.Gammon, 2)]
    [InlineData(GameResult.Backgammon, 3)]
    [InlineData(GameResult.LostSingle, -1)]
    [InlineData(GameResult.LostGammon, -2)]
    [InlineData(GameResult.LostBackgammon, -3)]
    [InlineData(GameResult.Draw, 0)]
    [InlineData(GameResult.Unknown, null)]
    public void OutcomePointsFollowModelAColor(GameResult whiteResult, int? whitePoints)
    {
        if (!whitePoints.HasValue)
        {
            var score = new TournamentScore();
            Assert.Throws<ArgumentOutOfRangeException>(() => score.RecordResult(new(whiteResult, 10, false), true));
            Assert.Equal(0, score.CompletedGames);
            Assert.Equal(0, score.ModelATotalPoints);
            return;
        }

        Assert.Equal(whitePoints.Value, TournamentRunner.GetModelAPoints(whiteResult, true));
        Assert.Equal(-whitePoints.Value, TournamentRunner.GetModelAPoints(whiteResult, false));

        foreach (var modelAIsWhite in new[] { true, false })
        {
            var points = modelAIsWhite ? whitePoints.Value : -whitePoints.Value;
            var score = new TournamentScore();
            score.RecordResult(new(whiteResult, 10, false), modelAIsWhite);

            Assert.Equal(points == 2 ? 1 : 0, score.ModelAGammonWins);
            Assert.Equal(points == 3 ? 1 : 0, score.ModelABackgammonWins);
            Assert.Equal(points == -2 ? 1 : 0, score.ModelBGammonWins);
            Assert.Equal(points == -3 ? 1 : 0, score.ModelBBackgammonWins);
        }
    }

    [Theory]
    [InlineData(GameResult.Backgammon)]
    [InlineData(GameResult.LostBackgammon)]
    [InlineData(GameResult.Draw)]
    [InlineData(GameResult.Unknown)]
    public void TerminalBoardsPreserveDrawsAndLastTurnWinsWithoutCubeMultipliers(GameResult expectedResult)
    {
        var fields = new int[24];
        var pins = new int[24];
        fields[0] = -15;
        fields[23] = 15;
        if (expectedResult == GameResult.Backgammon)
            fields[0] = 0;
        else if (expectedResult == GameResult.LostBackgammon)
            fields[23] = 0;

        if (expectedResult == GameResult.Draw)
        {
            fields[0] = 1;
            fields[23] = -1;
            fields[5] = -13;
            fields[18] = 13;
            pins[0] = -1;
            pins[23] = 1;
        }

        var board = BoardServiceFactory.Create(expectedResult == GameResult.Draw ? GameModus.Plakoto : GameModus.Backgammon)
            .CreateBoard(new BoardModelContract
            {
                Fields = fields,
                PinnedFields = pins,
                BearOffCountWhite = expectedResult == GameResult.Backgammon ? 15 : 0,
                BearOffCountBlack = expectedResult == GameResult.LostBackgammon ? 15 : 0,
                DoublingCubeValue = 4,
            });

        var result = TournamentRunner.CreateGameResult(board, 250);

        Assert.Equal(expectedResult, result.WhiteResult);
        Assert.Equal(250, result.TurnCount);
        Assert.Equal(expectedResult == GameResult.Unknown, result.Discarded);
        if (expectedResult == GameResult.Unknown)
            return;

        foreach (var modelAIsWhite in new[] { true, false })
        {
            var score = new TournamentScore();
            score.RecordResult(result, modelAIsWhite);
            Assert.Equal(expectedResult == GameResult.Draw ? 1 : 0, score.Draws);
            Assert.Equal(expectedResult == GameResult.Draw ? 0 : 3, Math.Abs(score.ModelATotalPoints));
        }
    }

    [Theory]
    [InlineData(0, 0.17457798444813344, 0.7654220155518665)]
    [InlineData(20, 0.1436835210239117, 0.6396498123094216)]
    public void PointEquityCanPromoteFewerWinsAndIncludesDrawsButExcludesDiscards(int draws, double expectedLower, double expectedUpper)
    {
        var score = new TournamentScore();
        for (var game = 0; game < 100; game++)
        {
            var modelAIsWhite = game % 2 == 0;
            var whiteResult = game < 49
                ? (modelAIsWhite ? GameResult.Gammon : GameResult.LostGammon)
                : (modelAIsWhite ? GameResult.LostSingle : GameResult.Single);
            score.RecordResult(new(whiteResult, 10, false), modelAIsWhite);
        }

        for (var game = 0; game < draws; game++)
            score.RecordResult(new(GameResult.Draw, 10, false), game % 2 == 0);

        var equity = TournamentRunner.ComputePointEquity(score.CompletedGames, score.ModelATotalPoints, score.ModelASquaredPoints);
        var result = new TournamentResult("A", "B", 100 + draws, score.ModelAWins, score.ModelBWins, score.Draws,
            score.Discarded, 10, score.ModelATotalPoints, score.ModelASquaredPoints);

        Assert.Equal(49, score.ModelAWins);
        Assert.Equal(51, score.ModelBWins);
        Assert.Equal(49, score.ModelAGammonWins);
        Assert.Equal(0, score.ModelABackgammonWins);
        Assert.Equal(0, score.ModelBGammonWins);
        Assert.Equal(0, score.ModelBBackgammonWins);
        Assert.Equal(draws, score.Draws);
        Assert.Equal(100 + draws, score.CompletedGames);
        Assert.Equal(47, score.ModelATotalPoints);
        Assert.Equal(247, score.ModelASquaredPoints);
        Assert.Equal(47d / (100 + draws), equity.PointsPerGame!.Value, 12);
        Assert.Equal(expectedLower, equity.LowerBound!.Value, 12);
        Assert.Equal(expectedUpper, equity.UpperBound!.Value, 12);
        Assert.StartsWith("A is STRONGER", TournamentRunner.DetermineVerdict(result, equity));

        var reversed = TournamentRunner.ComputePointEquity(score.CompletedGames, -score.ModelATotalPoints, score.ModelASquaredPoints);
        Assert.Equal(-equity.UpperBound.Value, reversed.LowerBound!.Value, 12);
        Assert.Equal(-equity.LowerBound.Value, reversed.UpperBound!.Value, 12);
        Assert.StartsWith("B is STRONGER", TournamentRunner.DetermineVerdict(result with
        {
            ModelAWins = result.ModelBWins,
            ModelBWins = result.ModelAWins,
            ModelATotalPoints = -result.ModelATotalPoints,
        }, reversed));

        score.RecordResult(new(GameResult.Unknown, 250, true), true);
        Assert.Equal(1, score.Discarded);
        Assert.Equal(49, score.ModelAGammonWins);
        Assert.Equal(0, score.ModelABackgammonWins);
        Assert.Equal(0, score.ModelBGammonWins);
        Assert.Equal(0, score.ModelBBackgammonWins);
        Assert.Equal(100 + draws, score.CompletedGames);
        Assert.Equal(47, score.ModelATotalPoints);
        Assert.Equal(247, score.ModelASquaredPoints);
    }

    [Theory]
    [InlineData(0.001, 0.01, 0, 0, "A is STRONGER")]
    [InlineData(-0.01, -0.001, 0, 0, "B is STRONGER")]
    [InlineData(-0.01, 0.01, 0, 0, "INCONCLUSIVE")]
    [InlineData(0, 0.01, 0, 0, "INCONCLUSIVE")]
    [InlineData(-0.01, 0, 0, 0, "INCONCLUSIVE")]
    [InlineData(0, 0, 0, 0, "INCONCLUSIVE")]
    [InlineData(0.001, 0.01, 1, 0, "INCONCLUSIVE")]
    [InlineData(0.001, 0.01, 0, 1, "INCONCLUSIVE")]
    public void VerdictUsesStrictPointEquityBoundsAndRequiresACompleteRun(
        double lower, double upper, int discarded, int unprocessed, string expectedVerdict)
    {
        var result = new TournamentResult("A", "B", 100 + discarded + unprocessed, 49, 51, 0, discarded, 10, 47, 247);
        var equity = new TournamentEquityStatistics((lower + upper) / 2, lower, upper);

        Assert.StartsWith(expectedVerdict, TournamentRunner.DetermineVerdict(result, equity));
    }

    [Theory]
    [InlineData(0, 0, 0, null)]
    [InlineData(1, 1, 1, 1d)]
    [InlineData(99, 0, 98, 0d)]
    [InlineData(100, 100, 100, 1d)]
    [InlineData(100, 0, 0, 0d)]
    public void InsufficientSamplesOrVariationDoNotProduceConfidenceBounds(
        int completedGames, long totalPoints, long squaredPoints, double? expectedMean)
    {
        var equity = TournamentRunner.ComputePointEquity(completedGames, totalPoints, squaredPoints);
        var result = new TournamentResult("A", "B", completedGames, completedGames, 0, 0, 0, 10, totalPoints, squaredPoints);

        Assert.Equal(expectedMean, equity.PointsPerGame);
        Assert.Null(equity.LowerBound);
        Assert.Null(equity.UpperBound);
        Assert.StartsWith("INCONCLUSIVE", TournamentRunner.DetermineVerdict(result, equity));
    }
}
