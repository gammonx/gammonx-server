using GammonX.Engine.Extensions;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Features;
using GammonX.Mars.NN.Services;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Tests.Features;

public sealed class FeatureBoardGeometryTests
{
    public static IEnumerable<object[]> Modes() =>
        new[] { GameModus.Backgammon, GameModus.Tavla, GameModus.Portes, GameModus.Fevga, GameModus.Plakoto }
            .Select(mode => new object[] { mode });

    public static IEnumerable<object[]> Players() =>
        Modes().SelectMany(row => new[] { true, false }.Select(white => new object[] { row[0], white }));

    public static IEnumerable<object[]> BarPlayers() =>
        Players().Where(row => (GameModus)row[0] != GameModus.Plakoto);

    [Theory]
    [MemberData(nameof(Players))]
    public void EveryBoardPointHasExactBearOffAndHomeEntryDistances(GameModus mode, bool white)
    {
        // Independent reference paths enumerate the actual board points travelled by each side.
        var path = Path(mode, white);
        for (var step = 0; step < path.Length; step++)
        {
            var board = Create(mode, [(path[step], white ? -1 : 1)]);
            Assert.Equal(24 - step, white ? board.PipCountWhite : board.PipCountBlack);
            Assert.Equal(24d - step, new AverageDistanceToBearOffFeature().Eval(board, white));
            Assert.Equal(Math.Max(18 - step, 0), new PipsToBearOffFeature().Eval(board, white));
        }
    }

    [Theory]
    [MemberData(nameof(BarPlayers))]
    public void BarAndReserveIncludeTheEntryPipAndRemainInTheAverage(GameModus mode, bool white)
    {
        var path = Path(mode, white);
        var board = Create(mode, [(path[0], white ? -3 : 3), (path[23], white ? -1 : 1)],
            whiteBar: white ? 2 : 0, blackBar: white ? 0 : 2);

        // Two on bar (25 each), three at start (24 each), one at last point (1).
        Assert.Equal(123, white ? board.PipCountWhite : board.PipCountBlack);
        Assert.Equal(123d / 6, new AverageDistanceToBearOffFeature().Eval(board, white));
        Assert.Equal(92, new PipsToBearOffFeature().Eval(board, white)); // 2*19 + 3*18
        Assert.Equal(-123, new PipDifferenceFeature().Eval(board, white));
    }

    [Theory]
    [InlineData(true, 18, 8, 0)]
    [InlineData(false, 18, 8, 0)]
    [InlineData(true, 5, 21, 13)]
    [InlineData(false, 5, 21, 13)]
    public void PinnedCheckerContributesItsOwnDistance(bool white, int pinPoint, int expectedPips, int expectedHomePips)
    {
        var board = Create(GameModus.Plakoto, [(pinPoint, 1), (22, -1)], [(pinPoint, -1)]);
        if (!white)
            board = board.InvertBoard();

        Assert.Equal(expectedPips, white ? board.PipCountWhite : board.PipCountBlack);
        Assert.Equal(expectedPips / 2d, new AverageDistanceToBearOffFeature().Eval(board, white));
        Assert.Equal(expectedHomePips, new PipsToBearOffFeature().Eval(board, white));
        Assert.Equal(expectedHomePips, new MotherDistanceFeature().Eval(board, white));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FevgaPrimeMayCrossPhysicalBoundaryButNotTheEndOfItsMovementPath(bool white)
    {
        var crossing = Create(GameModus.Fevga,
            [(4, -3), (9, -2), (10, -2), (11, -2), (12, -2), (13, -2), (14, -2), (18, 15)]);
        var separated = Create(GameModus.Fevga,
            [(0, -3), (1, -3), (10, -3), (18, 15), (22, -3), (23, -3)]);
        if (!white)
        {
            crossing = crossing.InvertBoard();
            separated = separated.InvertBoard();
        }

        Assert.Equal(6, new MaxPrimeLengthFeature().Eval(crossing, white));
        Assert.Equal(2, new MaxPrimeLengthFeature().Eval(separated, white));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LongestPrimeIsNotCappedAtSixAndCountsPointsHoldingPins(bool white)
    {
        var seven = Create(GameModus.Backgammon,
            [(0, -2), (1, -2), (2, -2), (3, -2), (4, -2), (5, -2), (6, -2), (7, -1), (23, 15)]);
        var pins = Create(GameModus.Plakoto, [(6, -1), (7, -1), (8, -1), (18, -12), (23, 12)],
            [(6, 1), (7, 1), (8, 1)]);
        if (!white)
        {
            seven = seven.InvertBoard();
            pins = pins.InvertBoard();
        }

        Assert.Equal(7, new MaxPrimeLengthFeature().Eval(seven, white));
        Assert.Equal(3, new MaxPrimeLengthFeature().Eval(pins, white));
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void RaceMatchesRemainingPathsForEveryPairOfOccupiedPoints(GameModus mode)
    {
        var whitePath = Path(mode, true);
        var blackPath = Path(mode, false);
        var feature = new RaceFeature();
        for (var whiteStep = 0; whiteStep < 24; whiteStep++)
        for (var blackStep = 0; blackStep < 24; blackStep++)
        {
            if (whitePath[whiteStep] == blackPath[blackStep])
                continue; // Opposing visible stacks cannot share a point.

            var board = Create(mode, [(whitePath[whiteStep], -15), (blackPath[blackStep], 15)]);
            var expected = !whitePath.Skip(whiteStep).Intersect(blackPath.Skip(blackStep)).Any();
            Assert.Equal(expected, feature.Eval(board, true));
            Assert.Equal(expected, feature.Eval(board, false));
            Assert.Equal(expected, feature.Eval(board.InvertBoard(), false));
        }
    }

    [Theory]
    [MemberData(nameof(BarPlayers))]
    public void BarPreventsRaceUntilTheOtherSideHasFinished(GameModus mode, bool white)
    {
        var board = Create(mode, [(white ? 2 : 21, white ? 15 : -15)],
            whiteBar: white ? 1 : 0, blackBar: white ? 0 : 1);
        var feature = new RaceFeature();
        Assert.False(feature.Eval(board, white));
        Assert.False(feature.Eval(board.InvertBoard(), !white));

        var finished = Create(mode, [], whiteBar: white ? 1 : 0, blackBar: white ? 0 : 1);
        Assert.True(feature.Eval(finished, white));
    }

    [Fact]
    public void HiddenPinnedCheckerPreventsFalseRace()
    {
        var board = Create(GameModus.Plakoto, [(2, 14), (5, 1), (20, -14)], [(5, -1)]);
        Assert.False(new RaceFeature().Eval(board, true));
        Assert.False(new RaceFeature().Eval(board.InvertBoard(), false));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LastPinIsTheOneNearestBearingOff(bool white)
    {
        var board = Create(GameModus.Plakoto, [(18, -1), (20, -13), (22, -1), (23, 13)],
            [(18, 1), (22, 1)]);
        if (!white)
            board = board.InvertBoard();

        Assert.Equal(14, new NumbersOfCheckersInFrontOfLastPinFeature().Eval(board, white));
        var noHomePin = Create(GameModus.Plakoto, [(6, -1), (20, -14), (23, 14)], [(6, 1)]);
        Assert.Equal(0, new NumbersOfCheckersInFrontOfLastPinFeature().Eval(noHomePin, true));
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void AnchorsMustBeAheadOfAnActualRemainingOpponent(GameModus mode)
    {
        var board = Create(mode, [(3, 15), (18, -2)]);
        var finishedOpponent = Create(mode, [(18, -2)]);
        var feature = new AnchorCountInFrontFeature();
        Assert.Equal(0, feature.Eval(board, true));
        Assert.Equal(0, feature.Eval(finishedOpponent, true));
        Assert.Equal(0, feature.Eval(board.InvertBoard(), false));
    }

    [Fact]
    public void AnchorFrontIncludesPinnedRearOpponentButExcludesItsOwnPinningPoint()
    {
        var board = Create(GameModus.Plakoto, [(2, 14), (10, -2), (18, -1)], [(18, 1)]);
        Assert.Equal(1, new AnchorCountInFrontFeature().Eval(board, true));
        Assert.Equal(1, new AnchorCountInFrontFeature().Eval(board.InvertBoard(), false));
    }

    [Theory]
    [MemberData(nameof(BarPlayers))]
    public void EveryMadePointIsAheadOfOpponentOnBar(GameModus mode, bool white)
    {
        var board = Create(mode, [(3, 14), (18, -2)], blackBar: 1);
        if (!white)
            board = board.InvertBoard();
        Assert.Equal(1, new AnchorCountInFrontFeature().Eval(board, white));
    }

    [Fact]
    public void FevgaInversionSwapsUnequalReservesAndPreservesRoundTrip()
    {
        var board = Create(GameModus.Fevga, [(1, -2), (14, 3)], whiteBar: 4, blackBar: 7);
        var inverted = board.InvertBoard();
        var bar = Assert.IsAssignableFrom<IHomeBarModel>(inverted);
        Assert.Equal(7, bar.HomeBarCountWhite);
        Assert.Equal(4, bar.HomeBarCountBlack);
        Assert.Equal(board.PipCountBlack, inverted.PipCountWhite);
        Assert.Equal(board.PipCountWhite, inverted.PipCountBlack);
        Assert.Equal(board.BearOffCountBlack, inverted.BearOffCountWhite);
        Assert.Equal(board.BearOffCountWhite, inverted.BearOffCountBlack);
        Assert.Equal(board, inverted.InvertBoard());
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void FinishedSideHasZeroDistanceAndNoLastPin(GameModus mode)
    {
        var board = Create(mode, []);
        foreach (var white in new[] { true, false })
        {
            Assert.Equal(0, new PipsToBearOffFeature().Eval(board, white));
            Assert.Equal(0d, new AverageDistanceToBearOffFeature().Eval(board, white));
            Assert.Equal(0, new NumbersOfCheckersInFrontOfLastPinFeature().Eval(board, white));
            Assert.True(new RaceFeature().Eval(board, white));
        }
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public void RecalculatedModelsAndVectorsMatchEquivalentPerspectives(GameModus mode)
    {
        var board = mode switch
        {
            GameModus.Fevga => Create(mode, [(9, -2), (10, -1), (11, -1), (12, -1), (13, -1), (19, 2)], whiteBar: 3, blackBar: 7),
            GameModus.Plakoto => Create(mode, [(2, 2), (6, -2), (10, 1), (18, -1), (19, 2), (20, -3), (22, -1)], [(10, -1), (18, 1), (22, 1)]),
            _ => Create(mode, [(0, -2), (5, 3), (7, -3), (16, 4), (18, -2)], whiteBar: 1, blackBar: 2)
        };
        var inverted = board.InvertBoard();
        var service = FeatureEvalServiceFactory.Create(mode, null!);
        var extractor = FeatureVectorExtractorFactory.Create(mode);
        foreach (var white in new[] { true, false })
        {
            var model = service.EvalPositionForTraining(new FeatureEvalOptions
            {
                BoardContract = board.ToContract(false), IsWhite = white, TurnNumber = 17
            });
            var invertedModel = service.EvalPositionForTraining(new FeatureEvalOptions
            {
                BoardContract = inverted.ToContract(false), IsWhite = !white, TurnNumber = 17
            });
            Assert.Equal(model, invertedModel);
            Assert.Equal(extractor.Extract(model, board, white), extractor.Extract(invertedModel, inverted, !white));
        }
    }

    private static int[] Path(GameModus mode, bool white) => white
        ? Enumerable.Range(0, 24).ToArray()
        : mode == GameModus.Fevga
            ? Enumerable.Range(12, 12).Concat(Enumerable.Range(0, 12)).ToArray()
            : Enumerable.Range(0, 24).Reverse().ToArray();

    private static IBoardModel Create(GameModus mode, (int Point, int Count)[] fields,
        (int Point, int Count)[]? pins = null, int whiteBar = 0, int blackBar = 0)
    {
        var contract = new BoardModelContract
        {
            Fields = new int[24], PinnedFields = mode == GameModus.Plakoto ? new int[24] : null,
            HomeBarCountWhite = whiteBar, HomeBarCountBlack = blackBar
        };
        foreach (var (point, count) in fields)
            contract.Fields[point] = count;
        foreach (var (point, count) in pins ?? [])
        {
            Assert.NotNull(contract.PinnedFields);
            Assert.Equal(-Math.Sign(count), Math.Sign(contract.Fields[point]));
            contract.PinnedFields[point] = count;
        }

        var checkers = contract.Fields.Concat(contract.PinnedFields ?? []);
        var remainingWhite = -checkers.Where(count => count < 0).Sum() + whiteBar;
        var remainingBlack = checkers.Where(count => count > 0).Sum() + blackBar;
        Assert.InRange(remainingWhite, 0, 15);
        Assert.InRange(remainingBlack, 0, 15);
        contract.BearOffCountWhite = 15 - remainingWhite;
        contract.BearOffCountBlack = 15 - remainingBlack;
        return BoardServiceFactory.Create(mode).CreateBoard(contract);
    }
}
