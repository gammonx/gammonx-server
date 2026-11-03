using GammonX.Engine.Models;
using GammonX.Mars.NN.Services;

namespace GammonX.Mars.NN.Tests.Services;

public partial class TwoPlySearchEvaluatorTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationAfterScoringRestoresRepliesAndStopsBeforeTheNextRoll(bool prepared)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var harness = CreateHarness(_ => [CreateSequence(1), CreateSequence(2)]);
        var clones = new List<IBoardModel>();
        var evaluator = new TwoPlySearchEvaluator(harness.BoardService.Object,
            (boards, _) =>
            {
                cancellation.Cancel();
                return Task.FromResult(new double[boards.Count]);
            }, board =>
            {
                var clone = CloneBoard(board);
                clones.Add(clone);
                return clone;
            }, maxBatchSize: 2, batchAcrossRolls: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => prepared
            ? evaluator.EvaluatePreparedAsync(harness.Board.Object, true,
                (board, _) => new PreparedPosition([board.Fields[0]]), positions =>
                {
                    cancellation.Cancel();
                    return Task.FromResult(new double[positions.Count]);
                }, cancellation.Token)
            : evaluator.EvaluateAsync(harness.Board.Object, true, cancellation.Token));

        Assert.Single(harness.RequestedRolls);
        Assert.Equal(0, harness.Fields[0]);
        Assert.NotEmpty(clones);
        Assert.All(clones, board => Assert.Equal(0, board.Fields[0]));
    }
}
