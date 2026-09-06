using GammonX.Mars.Training.Generator;

namespace GammonX.Mars.Training.Tests;

public sealed class SelectiveTwoPlyPolicyTests
{
    private static readonly SelectiveTwoPlyOptions Options = new()
    {
        Enabled = true,
        MaximumOnePlyGap = 0.02d,
        MaximumCandidates = 3,
        FullSearchAuditProbability = 0.01f
    };

    [Fact]
    public void SelectDeepensTopCandidatesWhenGapIsAtThreshold()
    {
        var decision = SelectiveTwoPlyPolicy.Select(5, 0.02d, 0.5f, Options);

        Assert.True(decision.ShouldEvaluate);
        Assert.Equal(SelectiveTwoPlyReason.Ambiguous, decision.Reason);
        Assert.Equal(3, decision.CandidateCount);
    }

    [Fact]
    public void SelectUsesAllCandidatesForAudit()
    {
        var decision = SelectiveTwoPlyPolicy.Select(5, 0.03d, 0.009f, Options);

        Assert.True(decision.ShouldEvaluate);
        Assert.Equal(SelectiveTwoPlyReason.Audit, decision.Reason);
        Assert.Equal(5, decision.CandidateCount);
    }

    [Fact]
    public void AuditUsesAllCandidatesEvenWhenPositionIsAmbiguous()
    {
        var decision = SelectiveTwoPlyPolicy.Select(5, 0.01d, 0.009f, Options);

        Assert.Equal(SelectiveTwoPlyReason.Audit, decision.Reason);
        Assert.Equal(5, decision.CandidateCount);
    }

    [Fact]
    public void SelectSkipsLargeGapOutsideAuditWindow()
    {
        var decision = SelectiveTwoPlyPolicy.Select(5, 0.03d, 0.01f, Options);

        Assert.False(decision.ShouldEvaluate);
        Assert.Equal(SelectiveTwoPlyReason.GapTooLarge, decision.Reason);
        Assert.Equal(0, decision.CandidateCount);
    }

    [Fact]
    public void SelectSkipsSingleCandidatePosition()
    {
        var decision = SelectiveTwoPlyPolicy.Select(1, null, 0f, Options);

        Assert.False(decision.ShouldEvaluate);
        Assert.Equal(SelectiveTwoPlyReason.SingleCandidate, decision.Reason);
    }

    [Fact]
    public void SelectSkipsWhenDisabled()
    {
        var decision = SelectiveTwoPlyPolicy.Select(5, 0.01d, 0f, Options with { Enabled = false });

        Assert.False(decision.ShouldEvaluate);
        Assert.Equal(SelectiveTwoPlyReason.Disabled, decision.Reason);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(-0.01d)]
    public void SelectRejectsInvalidGap(double scoreGap)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SelectiveTwoPlyPolicy.Select(2, scoreGap, 0.5f, Options));
    }

    [Fact]
    public void ValidateRejectsInvalidConfiguration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => (Options with { MaximumCandidates = 1 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(
            () => (Options with { MaximumOnePlyGap = double.PositiveInfinity }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(
            () => (Options with { FullSearchAuditProbability = 1.01f }).Validate());
    }
}