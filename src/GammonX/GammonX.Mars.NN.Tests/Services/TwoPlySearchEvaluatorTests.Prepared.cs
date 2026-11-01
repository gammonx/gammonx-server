using GammonX.Engine.Models;
using GammonX.Mars.NN.Services;
using Moq;

namespace GammonX.Mars.NN.Tests.Services;

public partial class TwoPlySearchEvaluatorTests
{
    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 4)]
    [InlineData(true, 1)]
    [InlineData(true, 4)]
    [InlineData(true, 32)]
    public async Task PreparedRowsMatchClonedRepliesAndUseOnlyOneWorkingBoard(bool crossRoll, int cap)
    {
        var harness = CreateHarness(rolls => rolls[0] == 3 ? [] :
            [CreateSequence(rolls[0], rolls[^1] + 10), CreateSequence(rolls[^1], rolls[0] + 20)], initialMarker: 7);
        var referenceRows = new List<double>();
        var reference = await new TwoPlySearchEvaluator(harness.BoardService.Object,
            (boards, _) =>
            {
                var scores = boards.Select(board => (double)board.Fields[0]).ToArray();
                referenceRows.AddRange(scores);
                return Task.FromResult(scores);
            }, CloneBoard, cap, crossRoll).EvaluateAsync(harness.Board.Object, true);
        var clones = new List<IBoardModel>();
        var rows = new List<double>();
        var evaluator = new TwoPlySearchEvaluator(harness.BoardService.Object,
            (_, _) => throw new Exception("Prepared path must not score boards."),
            board => { var clone = CloneBoard(board); clones.Add(clone); return clone; }, cap, crossRoll);

        var score = await evaluator.EvaluatePreparedAsync(harness.Board.Object, true,
            (board, white) =>
            {
                Assert.False(white);
                return new PreparedPosition([board.Fields[0]]);
            }, async positions =>
            {
                Assert.Equal(7, Assert.Single(clones).Fields[0]);
                Assert.InRange(positions.Count, 1, cap);
                await Task.Yield();
                var values = positions.Select(row => (double)row.Features![0]).ToArray();
                rows.AddRange(values);
                return values;
            });

        Assert.Equal(referenceRows, rows);
        Assert.Equal(BitConverter.DoubleToInt64Bits(reference), BitConverter.DoubleToInt64Bits(score));
        Assert.Equal(7, Assert.Single(clones).Fields[0]);
        Assert.Equal(7, harness.Fields[0]);
    }

    [Fact]
    public async Task PreparedRowsRemainIndependentAndAdmissionStopsDuringAwait()
    {
        var harness = CreateHarness(_ => [CreateSequence(1), CreateSequence(2)]);
        IBoardModel? working = null;
        var prepared = 0;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var evaluator = new TwoPlySearchEvaluator(harness.BoardService.Object, (_, _) => throw new Exception(),
            board => working = CloneBoard(board), 4, true);
        var evaluation = evaluator.EvaluatePreparedAsync(harness.Board.Object, true,
            (board, _) => { prepared++; return new PreparedPosition([board.Fields[0]]); },
            async rows =>
            {
                await release.Task;
                Assert.Equal(0, working!.Fields[0]);
                var values = rows.Select(row => (double)row.Features![0]).ToArray();
                Assert.Equal(Enumerable.Range(0, rows.Count).Select(index => (double)(index % 2 + 1)), values);
                return values;
            });
        try
        {
            Assert.False(evaluation.IsCompleted);
            Assert.Equal(4, prepared);
            Assert.Equal(2, harness.RequestedRolls.Count);
            Assert.Equal(0, working!.Fields[0]);
        }
        finally
        {
            release.TrySetResult();
            await evaluation;
        }
        Assert.Equal(42, prepared);
    }

    [Theory]
    [InlineData("prepare")]
    [InlineData("generation")]
    [InlineData("move")]
    [InlineData("clone")]
    [InlineData("scorer")]
    [InlineData("count")]
    [InlineData("cancel")]
    public async Task PreparedPathRestoresBoardOnFailure(string failure)
    {
        var harness = CreateHarness(rolls =>
        {
            if (failure == "generation" && rolls[1] == 2)
                throw new InvalidDataException("generation");
            return [CreateSequence(1, 2)];
        });
        if (failure == "move")
            harness.BoardService.Setup(service => service.MoveCheckerTo(It.IsAny<IBoardModel>(), 1, 2, false))
                .Throws(new InvalidDataException("move before mutation"));
        IBoardModel? working = null;
        var evaluator = new TwoPlySearchEvaluator(harness.BoardService.Object, (_, _) => throw new Exception(),
            board => failure == "clone" ? throw new InvalidDataException("clone") : working = CloneBoard(board), 4, true);
        var error = await Record.ExceptionAsync(() => evaluator.EvaluatePreparedAsync(harness.Board.Object, true,
            (board, _) => failure == "prepare" ? throw new InvalidDataException("prepare") : new PreparedPosition([board.Fields[0]]),
            async _ =>
            {
                await Task.Yield();
                Assert.Equal(0, working!.Fields[0]);
                if (failure == "scorer") throw new InvalidDataException("scorer");
                if (failure == "cancel") throw new OperationCanceledException(new CancellationToken(true));
                return [];
            }));
        Assert.NotNull(error);
        if (failure == "cancel") Assert.IsAssignableFrom<OperationCanceledException>(error);
        else Assert.IsType<InvalidDataException>(error);
        if (working != null) Assert.Equal(0, working.Fields[0]);
        Assert.Equal(0, harness.Fields[0]);
    }
}
