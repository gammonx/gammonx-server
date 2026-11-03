using GammonX.Engine.Models;
using GammonX.Mars.NN.Services;
using Moq;

namespace GammonX.Mars.NN.Tests.Services;

public partial class TwoPlySearchEvaluatorTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(32)]
    [InlineData(64)]
    public async Task PacksRollsAndPassesWithoutChangingRowOrderOrWeightedMaxima(int cap)
    {
        var expectedRows = new List<int>();
        var expectedScore = 0d;
        for (var first = 1; first <= 6; first++)
        {
            for (var second = first; second <= 6; second++)
            {
                var key = first * 10 + second;
                var rows = Markers(key);
                expectedRows.AddRange(rows);
                expectedScore -= (first == second ? 1d : 2d) * rows.Max() / 36d;
            }
        }

        var harness = CreateHarness(rolls =>
        {
            var key = rolls[0] * 10 + rolls[1];
            return key % 3 == 0 ? [] : Markers(key).Select(marker => CreateSequence(marker)).ToArray();
        }, initialMarker: 7);
        var actualRows = new List<int>();
        var sizes = new List<int>();
        var evaluator = new TwoPlySearchEvaluator(harness.BoardService.Object, async (boards, white) =>
        {
            Assert.False(white);
            await Task.Yield();
            sizes.Add(boards.Count);
            actualRows.AddRange(boards.Select(board => board.Fields[0]));
            return boards.Select(board => (double)board.Fields[0]).ToArray();
        }, CloneBoard, cap, batchAcrossRolls: true);

        var score = await evaluator.EvaluateAsync(harness.Board.Object, true, TestContext.Current.CancellationToken);

        Assert.Equal(expectedRows, actualRows);
        Assert.Equal(BitConverter.DoubleToInt64Bits(expectedScore), BitConverter.DoubleToInt64Bits(score));
        Assert.Equal((expectedRows.Count + cap - 1) / cap, sizes.Count);
        Assert.All(sizes.SkipLast(1), size => Assert.Equal(cap, size));
        Assert.InRange(sizes[^1], 1, cap);
        Assert.Equal(21, harness.RequestedRolls.Count);
        Assert.Equal(7, harness.Fields[0]);

        static int[] Markers(int key) => key % 3 == 0
            ? [7]
            : Enumerable.Range(0, 35 + key % 11).Select(index => key * 100 + index).ToArray();
    }

    [Fact]
    public async Task DoesNotAdmitAnotherReplyOrRollWhileScoringIsPending()
    {
        var harness = CreateHarness(_ => [CreateSequence(1)]);
        var clones = new List<IBoardModel>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var evaluator = new TwoPlySearchEvaluator(harness.BoardService.Object, async (boards, _) =>
        {
            calls++;
            await release.Task;
            Assert.All(boards, reply => Assert.Equal(1, reply.Fields[0]));
            return Enumerable.Repeat(1d, boards.Count).ToArray();
        }, board => { var clone = CloneBoard(board); clones.Add(clone); return clone; }, maxBatchSize: 4, batchAcrossRolls: true);

        var evaluation = evaluator.EvaluateAsync(harness.Board.Object, true, TestContext.Current.CancellationToken);
        try
        {
            Assert.False(evaluation.IsCompleted);
            Assert.Equal(1, calls);
            Assert.Equal(4, clones.Count);
            Assert.Equal(4, harness.RequestedRolls.Count);
            Assert.All(clones, reply => Assert.Equal(1, reply.Fields[0]));
            Assert.Equal(0, harness.Fields[0]);
        }
        finally
        {
            release.TrySetResult();
            await evaluation;
        }

        Assert.Equal(6, calls);
        Assert.All(clones, reply => Assert.Equal(0, reply.Fields[0]));
    }

    [Theory]
    [InlineData("scorer")]
    [InlineData("cancellation")]
    [InlineData("count")]
    public async Task RestoresCrossRollGroupAfterScoringFailure(string failure)
    {
        var harness = CreateHarness(_ => [CreateSequence(1), CreateSequence(2)]);
        var clones = new List<IBoardModel>();
        var evaluator = new TwoPlySearchEvaluator(harness.BoardService.Object, async (boards, _) =>
        {
            await Task.Yield();
            Assert.Equal(new[] { 1, 2, 1, 2 }, boards.Select(board => board.Fields[0]));
            if (failure == "scorer")
                throw new InvalidDataException("Expected scoring failure.");
            if (failure == "cancellation")
                throw new OperationCanceledException(new CancellationToken(true));
            return [];
        }, board => { var clone = CloneBoard(board); clones.Add(clone); return clone; }, maxBatchSize: 4, batchAcrossRolls: true);

        var error = await Record.ExceptionAsync(() => evaluator.EvaluateAsync(harness.Board.Object, true, TestContext.Current.CancellationToken));

        Assert.NotNull(error);
        if (failure == "cancellation")
            Assert.IsAssignableFrom<OperationCanceledException>(error);
        else
            Assert.IsType<InvalidDataException>(error);
        Assert.Equal(2, harness.RequestedRolls.Count);
        Assert.Equal(4, clones.Count);
        Assert.All(clones, reply => Assert.Equal(0, reply.Fields[0]));
        Assert.Equal(0, harness.Fields[0]);
    }

    [Theory]
    [InlineData("generation")]
    [InlineData("clone")]
    [InlineData("move")]
    public async Task RestoresPartialGroupIfPreparingALaterRowFails(string failure)
    {
        var harness = CreateHarness(rolls =>
        {
            if (failure == "generation" && rolls[1] == 2)
                throw new InvalidOperationException("Expected generation failure.");
            return failure == "move" ? [CreateSequence(1, 999)] : [CreateSequence(1)];
        });
        if (failure == "move")
            harness.BoardService.Setup(service => service.MoveCheckerTo(It.IsAny<IBoardModel>(), 1, 999, false))
                .Throws(new InvalidOperationException("Expected move failure before mutation."));
        var clones = new List<IBoardModel>();
        var evaluator = new TwoPlySearchEvaluator(harness.BoardService.Object,
            (_, _) => throw new Exception("A partial group must not have been submitted."),
            board =>
            {
                if (failure == "clone" && clones.Count == 1)
                    throw new InvalidOperationException("Expected clone failure.");
                var clone = CloneBoard(board);
                clones.Add(clone);
                return clone;
            }, batchAcrossRolls: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => evaluator.EvaluateAsync(harness.Board.Object, true, TestContext.Current.CancellationToken));

        Assert.Single(clones);
        Assert.Equal(0, clones[0].Fields[0]);
        Assert.Equal(0, harness.Fields[0]);
    }
}
