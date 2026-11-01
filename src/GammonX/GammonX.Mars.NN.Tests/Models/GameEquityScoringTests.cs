using GammonX.Mars.NN.Models;

namespace GammonX.Mars.NN.Tests.Models;

public sealed class GameEquityScoringTests
{
    [Theory]
    [InlineData(1f, 0f, 0f, 0f, 0f, 1d)]
    [InlineData(1f, 1f, 0f, 0f, 0f, 2d)]
    [InlineData(1f, 1f, 1f, 0f, 0f, 3d)]
    [InlineData(0f, 0f, 0f, 0f, 0f, -1d)]
    [InlineData(0f, 0f, 0f, 1f, 0f, -2d)]
    [InlineData(0f, 0f, 0f, 1f, 1f, -3d)]
    [InlineData(0.75f, 0.25f, 0.125f, 0.125f, 0.0625f, 0.6875d)]
    [InlineData(0.25f, 0.75f, 0.5f, 0.875f, 0.9375f, -1.5d)]
    [InlineData(-1f, 2f, -1f, 2f, -1f, -2d)]
    [InlineData(float.NaN, 0f, 0f, 0f, 0f, 0d)]
    [InlineData(float.NaN, float.NaN, float.NaN, float.NaN, float.NaN, 0d)]
    [InlineData(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, 0f, 0f, 3d)]
    [InlineData(float.NegativeInfinity, 0f, 0f, float.PositiveInfinity, float.PositiveInfinity, -3d)]
    public void ScoreOnlyPreservesOutcomeAndProjectionSemantics(
        float win, float winGammon, float winBackgammon, float loseGammon, float loseBackgammon,
        double expected)
    {
        float[] predictions = [win, winGammon, winBackgammon, loseGammon, loseBackgammon];

        Assert.Equal(expected, GameEquityModel.CalculateEquity(predictions));
        Assert.Equal(expected, new GameEquityModel(new GameOutcomeModel(predictions)).Equity);
    }

    [Fact]
    public void ScoreOnlyMatchesDiagnosticModelBitForBitWithoutMutatingPredictions()
    {
        var random = new Random(20261101);
        float[] boundaries =
        [
            float.NaN, float.PositiveInfinity, float.NegativeInfinity,
            float.MinValue, float.MaxValue, -1f, -0f, 0f,
            float.Epsilon, -float.Epsilon, 0.5f, 1f,
            MathF.BitDecrement(1f), MathF.BitIncrement(1f)
        ];
        var predictions = new float[5];
        var inputBits = new int[5];

        for (var sample = 0; sample < 10_000; sample++)
        {
            for (var head = 0; head < predictions.Length; head++)
            {
                predictions[head] = (sample % 3) switch
                {
                    0 => random.NextSingle(),
                    1 => boundaries[random.Next(boundaries.Length)],
                    _ => BitConverter.Int32BitsToSingle((int)random.NextInt64(int.MinValue, (long)int.MaxValue + 1))
                };
                inputBits[head] = BitConverter.SingleToInt32Bits(predictions[head]);
            }

            var expected = new GameEquityModel(new GameOutcomeModel(predictions)).Equity;
            var actual = GameEquityModel.CalculateEquity(predictions);

            Assert.Equal(BitConverter.DoubleToInt64Bits(expected), BitConverter.DoubleToInt64Bits(actual));
            Assert.InRange(actual, -3d, 3d);
            for (var head = 0; head < predictions.Length; head++)
                Assert.Equal(inputBits[head], BitConverter.SingleToInt32Bits(predictions[head]));
        }
    }

    [Fact]
    public void ScoreOnlyRejectsNullLikeOutcomeModel()
    {
        var expected = Assert.Throws<ArgumentNullException>(() => new GameOutcomeModel(null!));
        var actual = Assert.Throws<ArgumentNullException>(() => GameEquityModel.CalculateEquity(null!));

        Assert.Equal(expected.ParamName, actual.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ScoreOnlyRejectsMissingHeadsLikeOutcomeModel(int count)
    {
        var predictions = new float[count];
        var expected = Assert.Throws<ArgumentException>(() => new GameOutcomeModel(predictions));
        var actual = Assert.Throws<ArgumentException>(() => GameEquityModel.CalculateEquity(predictions));

        Assert.Equal(expected.ParamName, actual.ParamName);
        Assert.Equal(expected.Message, actual.Message);
    }

    [Fact]
    public void ScoreOnlyIgnoresExtraHeadsLikeOutcomeModel()
    {
        float[] predictions = [0.75f, 0.25f, 0.125f, 0.125f, 0.0625f, float.NaN];
        var expected = new GameEquityModel(new GameOutcomeModel(predictions)).Equity;

        Assert.Equal(expected, GameEquityModel.CalculateEquity(predictions));
    }

    [Fact]
    public void ScoreOnlyDoesNotAllocateForValidOrInvalidProbabilities()
    {
        float[][] predictions =
        [
            [0.75f, 0.25f, 0.125f, 0.125f, 0.0625f],
            [-1f, 2f, -1f, 2f, -1f],
            [float.NaN, float.PositiveInfinity, float.NegativeInfinity, 1f, 0f]
        ];
        for (var warmup = 0; warmup < 1_000; warmup++)
            foreach (var row in predictions)
                GameEquityModel.CalculateEquity(row);

        var checksum = 0d;
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var iteration = 0; iteration < 1_000; iteration++)
            foreach (var row in predictions)
                checksum += GameEquityModel.CalculateEquity(row);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.True(double.IsFinite(checksum));
        Assert.Equal(0L, allocated);
    }
}
