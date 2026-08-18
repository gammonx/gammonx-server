namespace GammonX.Mars.Training.Tests;

public sealed class RankAwareExplorationPolicyTests
{
    private static readonly ExplorationOptions Options = new();

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
            RankAwareExplorationPolicy.Select(21, 500, true, 0.05f, Options));
        Assert.Equal(
            ExplorationChoice.Greedy,
            RankAwareExplorationPolicy.Select(1, 500, false, 0f, Options));
    }

    [Fact]
    public void GetRankedCandidateIndexClampsTopRankedCountToAvailableResults()
    {
        Assert.Equal(2, RankAwareExplorationPolicy.GetRankedCandidateIndex(3, 2, Options));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => RankAwareExplorationPolicy.GetRankedCandidateIndex(3, 3, Options));
    }
}