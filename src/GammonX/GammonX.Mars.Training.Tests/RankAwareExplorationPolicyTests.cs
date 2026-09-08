using GammonX.Mars.Training.Generator;

namespace GammonX.Mars.Training.Tests;

public sealed class RankAwareExplorationPolicyTests
{
    private static readonly ExplorationOptions Options = new();

    private static readonly ExplorationOptions ScoreGapOptions = new()
    {
        ScoreGapAwareExplorationEnabled = true,
        ScoreGapSmallThreshold = 0.1d,
        ScoreGapLargeThreshold = 0.5d,
        SmallGapRankedExplorationMultiplier = 2f,
        LargeGapRankedExplorationMultiplier = 0.5f
    };

    [Fact]
    public void Select_UsesAllLegalBranchForTheRareProbabilityWindow()
    {
        Assert.Equal(
            ExplorationChoice.AllLegal,
            RankAwareExplorationPolicy.Select(1, 500, true, 0.009f, Options));
    }

    [Fact]
    public void Select_UsesRankedBranchAfterAllLegalWindow()
    {
        Assert.Equal(
            ExplorationChoice.Ranked,
            RankAwareExplorationPolicy.Select(1, 500, true, 0.01f, Options));
        Assert.Equal(
            ExplorationChoice.Ranked,
            RankAwareExplorationPolicy.Select(1, 500, true, 0.249f, Options));
        Assert.Equal(
            ExplorationChoice.Ranked,
            RankAwareExplorationPolicy.Select(21, 500, true, 0.049f, Options));
    }

    [Fact]
    public void SelectFallsBackToGreedyOutsideConfiguredExplorationWindows()
    {
        Assert.Equal(
            ExplorationChoice.Greedy,
            RankAwareExplorationPolicy.Select(1, 500, true, 0.25f, Options));
        Assert.Equal(
            ExplorationChoice.Greedy,
            RankAwareExplorationPolicy.Select(21, 500, true, 0.09f, Options));
        Assert.Equal(
            ExplorationChoice.Greedy,
            RankAwareExplorationPolicy.Select(1, 500, false, 0f, Options));
    }

    [Fact]
    public void SelectIncreasesRankedExplorationForSmallScoreGaps()
    {
        Assert.Equal(
            ExplorationChoice.Ranked,
            RankAwareExplorationPolicy.Select(1, 500, true, 0.48f, ScoreGapOptions, scoreGap: 0.1d));
    }

    [Fact]
    public void SelectReducesRankedExplorationForLargeScoreGaps()
    {
        Assert.Equal(
            ExplorationChoice.Greedy,
            RankAwareExplorationPolicy.Select(1, 500, true, 0.13f, ScoreGapOptions, scoreGap: 0.5d));
    }

    [Fact]
    public void SelectInterpolatesRankedExplorationBetweenScoreGapThresholds()
    {
        Assert.Equal(
            ExplorationChoice.Ranked,
            RankAwareExplorationPolicy.Select(1, 500, true, 0.30f, ScoreGapOptions, scoreGap: 0.3d));
    }

    [Fact]
    public void SelectUsesPhaseProbabilityWhenScoreGapIsUnavailable()
    {
        Assert.Equal(
            ExplorationChoice.Greedy,
            RankAwareExplorationPolicy.Select(1, 500, true, 0.25f, ScoreGapOptions));
    }

    [Fact]
    public void SelectRejectsInvalidScoreGap()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => RankAwareExplorationPolicy.Select(1, 500, true, 0f, ScoreGapOptions, scoreGap: double.NaN));
    }

    [Fact]
    public void GetRankedCandidateIndexClampsTopRankedCountToAvailableResults()
    {
        Assert.Equal(2, RankAwareExplorationPolicy.GetRankedCandidateIndex(3, 2, Options));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => RankAwareExplorationPolicy.GetRankedCandidateIndex(3, 3, Options));
    }
}