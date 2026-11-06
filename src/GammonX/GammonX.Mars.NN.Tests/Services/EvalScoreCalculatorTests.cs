using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;
using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Tests.Services;

public sealed class EvalScoreCalculatorTests
{
    [Fact]
    public void CheapScoreUsesMatchingAverageDistanceWeights()
    {
        var weights = new ContactWeightModel
        {
            AverageDistanceToBearOffPlayerWeight = 0.7,
            AverageDistanceToBearOffOppWeight = 0.2,
        };
        var eval = new EvalResultModel
        {
            AverageDistanceToBearOffPlayer = 12.5,
            AverageDistanceToBearOffOpp = 25,
        };

        var (_, score) = EvalScoreCalculator.CalculateCheapScore(
            eval,
            weights,
            new RaceWeightModel(),
            GameModus.Backgammon);

        Assert.Equal(-0.15, score, precision: 10);
    }
}
