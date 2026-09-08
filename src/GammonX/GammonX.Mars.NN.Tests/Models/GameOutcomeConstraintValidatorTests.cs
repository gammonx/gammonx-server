using GammonX.Mars.NN.Models;

namespace GammonX.Mars.NN.Tests.Models;

public sealed class GameOutcomeConstraintValidatorTests
{
    [Fact]
    public void ValidCumulativePredictionsHaveNoViolations()
    {
        var report = GameOutcomeConstraintValidator.Validate([0.7f, 0.2f, 0.05f, 0.1f, 0.02f]);

        Assert.True(report.IsValid);
        Assert.Equal(0, report.ViolationCount);
        Assert.Equal(0d, report.TotalViolation);
    }

    [Fact]
    public void ValidatorReportsEachHierarchyViolation()
    {
        var report = GameOutcomeConstraintValidator.Validate([0.4f, 0.6f, 0.8f, 0.9f, 0.95f]);

        Assert.False(report.IsValid);
        Assert.Equal(4, report.ViolationCount);
        Assert.Equal(0.2d, report.WinGammonHierarchyViolation, 5);
        Assert.Equal(0.2d, report.WinBackgammonHierarchyViolation, 5);
        Assert.Equal(0.3d, report.LoseGammonComplementViolation, 5);
        Assert.Equal(0.05d, report.LoseBackgammonHierarchyViolation, 5);
    }

    [Fact]
    public void ValidatorReportsRangeAndNonFiniteValues()
    {
        var report = GameOutcomeConstraintValidator.Validate([float.NaN, -0.1f, 1.1f, 0f, float.PositiveInfinity]);

        Assert.False(report.IsValid);
        Assert.True(report.HasNonFiniteValue);
        Assert.Equal(1d, report.RangeViolation, 5);
        Assert.True(report.ViolationCount >= 1);
    }

    [Fact]
    public void ProjectionPreservesValidValuesAndCorrectsInvalidValues()
    {
        var valid = GameOutcomeConstraintValidator.Project([0.7f, 0.2f, 0.05f, 0.1f, 0.02f]);
        var invalid = GameOutcomeConstraintValidator.Project([0.5f, 0.7f, 0.2f, 0.6f, 0.1f]);

        Assert.False(valid.WasProjected);
        Assert.Equal(0.7d, valid.WinP, 5);
        Assert.Equal(0.3d, valid.LoseP, 5);

        Assert.True(invalid.WasProjected);
        Assert.Equal(0.5d, invalid.WinGammonP, 5);
        Assert.Equal(0.2d, invalid.WinBackgammonP, 5);
        Assert.Equal(0.5d, invalid.LoseGammonP, 5);
        Assert.Equal(0.1d, invalid.LoseBackgammonP, 5);
    }

    [Fact]
    public void ValidatorRejectsWrongHeadCount()
    {
        Assert.Throws<ArgumentException>(() => GameOutcomeConstraintValidator.Validate([0.5f]));
    }
}