using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;

namespace GammonX.Mars.NN.Tests.Services;

public partial class TwoPlySearchEvaluatorTests
{
    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 4)]
    [InlineData(false, 32)]
    [InlineData(true, 1)]
    [InlineData(true, 4)]
    [InlineData(true, 32)]
    public async Task ReturnsDiceWeightedPredictionsOfTheBestEquityReply(bool prepared, int cap)
    {
        var harness = CreateHarness(rolls => rolls.Length == 4
            ? [CreateSequence(3), CreateSequence(1), CreateSequence(2)]
            : [CreateSequence(3), CreateSequence(1)]);
        float[][] predictions =
        [
            [0.8f, 0.1f, 0.02f, 0.1f, 0.02f],
            [0.6f, 0.55f, 0.2f, 0.05f, 0.01f],
            [0.4f, 0.2f, 0.05f, 0.3f, 0.1f]
        ];
        var originals = predictions.Select(row => row.ToArray()).ToArray();
        var result = await EvaluatePredictions(harness, prepared, cap, marker =>
            new EvalResult(GameEquityModel.CalculateEquity(predictions[marker - 1]), predictions[marker - 1]));

        Assert.NotNull(result.Prediction);
        // Six doubles select reply 2 despite its lower pWin; thirty other rolls select reply 1.
        float[] expected = [0.23333333f, 0.09166667f, 0.01833333f, 0.175f, 0.05f];
        Assert.Equal(5, result.Prediction.Length);
        for (var head = 0; head < expected.Length; head++)
            Assert.Equal(expected[head], result.Prediction[head], 6);
        Assert.Equal(-0.64833333d, result.Equity, 6);
        Assert.Equal(result.Equity, GameEquityModel.CalculateEquity(result.Prediction), 6);
        for (var row = 0; row < predictions.Length; row++)
            Assert.Equal(originals[row], predictions[row]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreservesFirstReplyPredictionsWhenEquitiesTie(bool prepared)
    {
        var harness = CreateHarness(_ => [CreateSequence(1), CreateSequence(2)]);
        var result = await EvaluatePredictions(harness, prepared, 1, marker =>
            new EvalResult(0.25d, marker == 1
                ? [0.625f, 0.125f, 0f, 0.125f, 0f]
                : [0.5f, 0.25f, 0f, 0f, 0f]));

        Assert.Equal(-0.25d, result.Equity, 12);
        Assert.Equal(new[] { 0.375f, 0.125f, 0f, 0.125f, 0f }, result.Prediction);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DoesNotInventPredictionsWhenASelectedRollHasNone(bool prepared)
    {
        var harness = CreateHarness(rolls => [CreateSequence(rolls.Length == 4 ? 1 : 2)]);
        var result = await EvaluatePredictions(harness, prepared, 4, marker => marker == 1
            ? new EvalResult(1d, null)
            : new EvalResult(0d, [0.5f, 0f, 0f, 0f, 0f]));

        Assert.Equal(-1d / 6d, result.Equity, 12);
        Assert.Null(result.Prediction);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SupportsOneHeadPredictionsMixedWithExactTerminalReplies(bool prepared)
    {
        var harness = CreateHarness(rolls => [CreateSequence(rolls.Length == 4 ? 1 : 2)]);
        var result = await EvaluatePredictions(harness, prepared, 4, marker => marker == 1
            ? new EvalResult(1d, [1f, 0f, 0f, 0f, 0f])
            : new EvalResult(0.6d, [0.6f]));

        Assert.Equal(-2d / 3d, result.Equity, 12);
        Assert.NotNull(result.Prediction);
        Assert.Equal(1f / 3f, Assert.Single(result.Prediction), 6);
    }

    private static Task<EvalResult> EvaluatePredictions(
        Harness harness, bool prepared, int cap, Func<int, EvalResult> evaluate)
    {
        var evaluator = new TwoPlySearchEvaluator(harness.BoardService.Object,
            (boards, _) => Task.FromResult(boards.Select(board => evaluate(board.Fields[0])).ToArray()),
            CloneBoard, cap, batchAcrossRolls: true);
        return prepared
            ? evaluator.EvalPreparedAsync(harness.Board.Object, true,
                (board, _) => new PreparedPosition([board.Fields[0]]),
                positions => Task.FromResult(positions.Select(position => evaluate((int)position.Features![0])).ToArray()))
            : evaluator.EvaluateAsync(harness.Board.Object, true);
    }
}
