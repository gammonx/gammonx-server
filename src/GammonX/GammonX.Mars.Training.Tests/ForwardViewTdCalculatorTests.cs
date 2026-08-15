using GammonX.Models.Enums;

namespace GammonX.Mars.Training.Tests;

public sealed class ForwardViewTdCalculatorTests
{
    [Fact]
    public void TerminalRewardsUseCumulativeOutcomeHeads()
    {
        AssertVector([1f, 0f, 0f, 0f, 0f], ForwardViewTdCalculator.GetTerminalReward(GameResult.Single));
        AssertVector([1f, 1f, 0f, 0f, 0f], ForwardViewTdCalculator.GetTerminalReward(GameResult.Gammon));
        AssertVector([1f, 1f, 1f, 0f, 0f], ForwardViewTdCalculator.GetTerminalReward(GameResult.Backgammon));
        AssertVector([0f, 0f, 0f, 0f, 0f], ForwardViewTdCalculator.GetTerminalReward(GameResult.LostSingle));
        AssertVector([0f, 0f, 0f, 1f, 0f], ForwardViewTdCalculator.GetTerminalReward(GameResult.LostGammon));
        AssertVector([0f, 0f, 0f, 1f, 1f], ForwardViewTdCalculator.GetTerminalReward(GameResult.LostBackgammon));
        AssertVector([0.5f, 0f, 0f, 0f, 0f], ForwardViewTdCalculator.GetTerminalReward(GameResult.Draw));
    }

    [Fact]
    public void PredictionPerspectiveInversionSwapsWinAndLossHeads()
    {
        var prediction = new[] { 0.6f, 0.2f, 0.1f, 0.15f, 0.05f };

        var inverted = ForwardViewTdCalculator.InvertPrediction(prediction);
        var roundTrip = ForwardViewTdCalculator.InvertPrediction(inverted);

        AssertVector([0.4f, 0.15f, 0.05f, 0.2f, 0.1f], inverted);
        AssertVector(prediction, roundTrip);
    }

    [Fact]
    public void LambdaOneProducesTerminalMonteCarloTargetsForEveryHead()
    {
        var trajectory = CreateThreeTurnGammonTrajectory();

        var labels = ForwardViewTdCalculator.Calculate(trajectory, lambda: 1f);

        AssertVector([1f, 1f, 0f, 0f, 0f], labels[0]);
        AssertVector([0f, 0f, 0f, 1f, 0f], labels[1]);
        AssertVector([1f, 1f, 0f, 0f, 0f], labels[2]);
    }

    [Fact]
    public void LambdaZeroUsesTheImmediatePerspectiveCorrectedPrediction()
    {
        var trajectory = CreateThreeTurnGammonTrajectory();

        var labels = ForwardViewTdCalculator.Calculate(trajectory, lambda: 0f);

        AssertVector([0.6f, 0.2f, 0.1f, 0.1f, 0.05f], labels[0]);
        AssertVector([0f, 0f, 0f, 1f, 0f], labels[1]);
        AssertVector([1f, 1f, 0f, 0f, 0f], labels[2]);
    }

    [Fact]
    public void LambdaHalfBlendsOneStepAndTerminalReturns()
    {
        var trajectory = CreateThreeTurnGammonTrajectory();

        var labels = ForwardViewTdCalculator.Calculate(trajectory, lambda: 0.5f);

        AssertVector([0.8f, 0.6f, 0.05f, 0.05f, 0.025f], labels[0]);
        AssertVector([0f, 0f, 0f, 1f, 0f], labels[1]);
        AssertVector([1f, 1f, 0f, 0f, 0f], labels[2]);
    }

    [Fact]
    public void GammaDiscountsFutureReturns()
    {
        var trajectory = CreateThreeTurnGammonTrajectory();

        var labels = ForwardViewTdCalculator.Calculate(trajectory, lambda: 0f, gamma: 0.5f);

        AssertVector([0.3f, 0.1f, 0.05f, 0.05f, 0.025f], labels[0]);
        AssertVector([0f, 0f, 0f, 0.5f, 0f], labels[1]);
        AssertVector([1f, 1f, 0f, 0f, 0f], labels[2]);
    }

    [Fact]
    public void OneHeadTrajectoriesOnlyReturnWinProbability()
    {
        var trajectory = CreateThreeTurnGammonTrajectory();

        var labels = ForwardViewTdCalculator.Calculate(trajectory, lambda: 1f, headCount: 1);

        AssertVector([1f], labels[0]);
        AssertVector([0f], labels[1]);
        AssertVector([1f], labels[2]);
    }

    [Fact]
    public void InvalidTrajectoryShapeIsRejected()
    {
        var positions = new[]
        {
            new TrajectoryPosition(0, true, [], [0.5f, 0.5f, 0.5f, 0.5f, 0.5f]),
            new TrajectoryPosition(1, true, [], [0.5f, 0.5f, 0.5f, 0.5f, 0.5f], true)
        };
        var trajectory = new GameTrajectory(
            Guid.NewGuid(),
            positions,
            GameResult.Single,
            GameResult.LostSingle,
            true);

        Assert.Throws<InvalidDataException>(() => ForwardViewTdCalculator.Calculate(trajectory, 0.5f));
    }

    private static GameTrajectory CreateThreeTurnGammonTrajectory()
    {
        var positions = new[]
        {
            new TrajectoryPosition(0, true, [], [0.5f, 0.2f, 0.1f, 0.1f, 0.1f]),
            new TrajectoryPosition(1, false, [], [0.4f, 0.1f, 0.05f, 0.2f, 0.1f]),
            new TrajectoryPosition(2, true, [], [0.6f, 0.3f, 0.15f, 0.05f, 0.05f], true)
        };

        return new GameTrajectory(
            Guid.NewGuid(),
            positions,
            GameResult.Gammon,
            GameResult.LostGammon,
            true);
    }

    private static void AssertVector(float[] expected, IReadOnlyList<float> actual)
    {
        Assert.Equal(expected.Length, actual.Count);
        for (var index = 0; index < expected.Length; index++)
            Assert.Equal(expected[index], actual[index], precision: 5);
    }
}
