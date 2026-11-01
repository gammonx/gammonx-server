using System.Collections.Concurrent;
using System.Threading.Channels;

using GammonX.Engine.Models;
using GammonX.Engine.Services;
using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;
using GammonX.Mars.NN.Tests.Performance;
using GammonX.Models.Enums;

using Moq;

namespace GammonX.Mars.NN.Tests.Services;

[Collection(nameof(PerformanceTestCollection))]
public class CandidateWindowTests
{
    [Theory]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    public async Task BoundsAdmissionAndPreservesTiesDespiteOutOfOrderCompletion(int window, bool batchAcrossRolls)
    {
        var (service, board, moves, neural) = Controlled(window);
        service.BatchAcrossRolls = batchAcrossRolls;
        var original = board.DeepClone();
        var evaluation = service.GetCandidatesByEvalAsync(board, moves, true, EvalWeights.DefaultContactWeights, moves.Length, BotLevel.Expert);
        for (var index = 1; index <= window; index++)
        {
            Assert.Equal(index, await neural.Started.Reader.ReadAsync(TestContext.Current.CancellationToken));
        }

        Assert.False(neural.Started.Reader.TryRead(out _));

        neural.Release(window);

        Assert.Equal(window + 1, await neural.Started.Reader.ReadAsync(TestContext.Current.CancellationToken));
        Assert.False(evaluation.IsCompleted);

        for (var index = 1; index <= moves.Length; index++)
        {
            neural.Release(index);
        }
        var result = (await evaluation).ToArray();

        Assert.Equal(moves, result.Select(row => row.MoveSequence));
        Assert.True(result[0].MoveSequence.Equals(moves[0]));
        Assert.InRange(neural.Peak, 2, window);
        Assert.Equal((batchAcrossRolls ? 1 : 21) * moves.Length, neural.Calls);
        Assert.True(board.Equals(original));
        Assert.All(neural.Boards.Values, owned =>
        {
            Assert.NotSame(board.Fields, owned.Fields);
            Assert.NotSame(board.History, owned.History);
            Assert.True(owned.Equals(original));
            Assert.Equal(original.History.Events.Count, owned.History.Events.Count);
        });
        Assert.Equal(0, neural.Active);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DrainsAllStartedCandidatesOnFailureOrCancellation(bool cancel)
    {
        var (service, board, moves, neural) = Controlled(2);
        var original = board.DeepClone();
        var evaluation = service.GetCandidatesByEvalAsync(board, moves, true, EvalWeights.DefaultContactWeights, moves.Length, BotLevel.Expert);

        Assert.Equal(1, await neural.Started.Reader.ReadAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, await neural.Started.Reader.ReadAsync(TestContext.Current.CancellationToken));

        neural.Fail(2, cancel);
        await neural.Returned.Reader.ReadAsync(TestContext.Current.CancellationToken);

        Assert.False(evaluation.IsCompleted);
        neural.Fail(1, cancel);
        var error = await Record.ExceptionAsync(() => evaluation);

        Assert.NotNull(error);
        Assert.Equal(cancel, error is OperationCanceledException);
        Assert.Equal(2, neural.Boards.Count);
        Assert.Equal(0, neural.Active);
        Assert.All(neural.Boards.Values, owned =>
        {
            Assert.True(owned.Equals(original));
            Assert.Equal(original.History.Events.Count, owned.History.Events.Count);
        });
        Assert.True(board.Equals(original));
    }

    public static IEnumerable<object[]> Variants() =>
        new[] { GameModus.Backgammon, GameModus.Portes, GameModus.Tavla, GameModus.Plakoto, GameModus.Fevga }
            .SelectMany(modus => new[] { 2, 4 }.SelectMany(window =>
                new[] { true, false }.Select(white => new object[] { modus, window, white })));

    [Theory]
    [MemberData(nameof(Variants))]
    public async Task MatchesSequentialScoresAndHistoryInputsForEveryVariant(GameModus modus, int window, bool white)
    {
        var boards = BoardServiceFactory.Create(modus);
        var board = boards.CreateBoard();
        for (var turn = 0; turn < 3; turn++)
        {
            boards.AddRollEventToHistory(board, turn % 2 == 0, [2, 3]);
        }
        var original = board.DeepClone();
        var moves = boards.GetUniqueLegalMoveSequences(board, white, [2, 3]).Take(4).ToArray();
        Assert.NotEmpty(moves);

        var neural = new FeatureScorer(modus);
        var service = Assert.IsAssignableFrom<BaseFeatureEvalServiceImpl>(FeatureEvalServiceFactory.Create(modus, neural));
        var reference = (await service.GetCandidatesByEvalAsync(board, moves, white, EvalWeights.DefaultContactWeights, moves.Length, BotLevel.Expert)).ToArray();
        
        var rows = neural.Rows.ToArray();
        neural.Rows.Clear();
        service.ParallelCandidateCount = window;
        service.ReuseReplyBoard = true;
        var actual = (await service.GetCandidatesByEvalAsync(board, moves, white, EvalWeights.DefaultContactWeights, moves.Length, BotLevel.Expert)).ToArray();

        Assert.Equal(reference.Select(row => row.MoveSequence), actual.Select(row => row.MoveSequence));
        for (var index = 0; index < reference.Length; index++)
        {
            Assert.InRange(Math.Abs(reference[index].Score - actual[index].Score), 0, 1e-5);
        }

        var expectedInputs = rows.GroupBy(row => row, NeuralInputComparer.Instance)
            .ToDictionary(group => group.Key, group => group.Count(), NeuralInputComparer.Instance);
        var actualInputs = neural.Rows.GroupBy(row => row, NeuralInputComparer.Instance)
            .ToDictionary(group => group.Key, group => group.Count(), NeuralInputComparer.Instance);
        Assert.Equal(expectedInputs.Count, actualInputs.Count);

        foreach (var (row, count) in expectedInputs)
        {
            Assert.Equal(count, actualInputs[row]);
        }

        Assert.True(board.Equals(original));
        Assert.Equal(original.History.Events.Count, board.History.Events.Count);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task TerminalCandidatesDoNotSubmitPredictions(int window)
    {
        var boards = BoardServiceFactory.Create(GameModus.Backgammon);
        var fields = new int[24];
        fields[23] = -1;
        fields[0] = 15;
        var board = boards.CreateBoard(new()
        {
            Fields = fields, BearOffCountWhite = 14
        });

        var moves = boards.GetUniqueLegalMoveSequences(board, true, [1]);
        var neural = new FeatureScorer(GameModus.Backgammon);
        var service = new DefaultFeatureEvalService(neural, GameModus.Backgammon) { ParallelCandidateCount = window };
        var result = await service.GetCandidatesByEvalAsync(board, moves, true, EvalWeights.DefaultContactWeights, moves.Length, BotLevel.Expert);
        
        Assert.All(result, row => Assert.True(row.Score > 0));
        Assert.NotEmpty(result);
        Assert.Empty(neural.Rows);
        Assert.Equal(-1, board.Fields[23]);
        Assert.Equal(14, board.BearOffCountWhite);
    }

    [Fact]
    public async Task ActualWorkerShutdownFinishesEveryProducer()
    {
        using var neural = Assert.IsType<BatchedNeuralEvalService>(BatchedNeuralEvalService.Load(
            GameModus.Backgammon, Path.Combine("Data", "NeuralNets", "Backgammon", "training_net.dat"),
            TorchSharp.torch.CPU));

        await neural.StartAsync(CancellationToken.None);

        try
        {
            var boards = BoardServiceFactory.Create(GameModus.Backgammon);
            var board = boards.CreateBoard();
            var original = board.DeepClone();
            var moves = boards.GetUniqueLegalMoveSequences(board, true, [2, 3]);
            var service = new DefaultFeatureEvalService(neural, GameModus.Backgammon) { ParallelCandidateCount = 4 };
            var evaluation = service.GetCandidatesByEvalAsync(
                board, moves, true, EvalWeights.DefaultContactWeights, moves.Length, BotLevel.Expert);
            await neural.StopAsync(CancellationToken.None);
            var error = await Record.ExceptionAsync(() => evaluation.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken));
            Assert.NotNull(error);
            Assert.IsNotType<TimeoutException>(error);
            Assert.True(board.Equals(original));
            await Assert.ThrowsAsync<InvalidOperationException>(() => neural.PredictBatchAsync([new float[216]]));
        }
        finally
        {
            await neural.StopAsync(CancellationToken.None);
        }
    }

    private static (ControlledService, IBoardModel, MoveSequenceModel[], GatedScorer) Controlled(int window)
    {
        var real = BoardServiceFactory.Create(GameModus.Backgammon);
        var board = real.CreateBoard();
        real.AddRollEventToHistory(board, true, [2, 3]);
        ((IDoublingCubeModel)board).AcceptDoublingCubeOffer(true);
        var mock = new Mock<IBoardService>();
        mock.Setup(service => service.GetUniqueLegalMoveSequences(It.IsAny<IBoardModel>(), It.IsAny<bool>(), It.IsAny<int[]>()))
            .Returns([]);
        mock.Setup(service => service.MoveCheckerTo(It.IsAny<IBoardModel>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .Callback<IBoardModel, int, int, bool>(real.MoveCheckerTo);
        mock.Setup(service => service.UndoMove(It.IsAny<IBoardModel>(), It.IsAny<MoveModel>(), It.IsAny<bool>()))
            .Callback<IBoardModel, MoveModel, bool>(real.UndoMove);
        var neural = new GatedScorer();
        var service = new ControlledService(neural, mock.Object) { ParallelCandidateCount = window, ReuseReplyBoard = true };
        var moves = Enumerable.Range(1, 5).Select(to =>
            new MoveSequenceModel { Moves = [new MoveModel(0, to)] }).ToArray();
        // Point 5 is occupied; use a different legal starting checker for the final candidate.
        moves[4] = new MoveSequenceModel { Moves = [new MoveModel(11, 13)] };
        return (service, board, moves, neural);
    }

    private sealed class ControlledService(INeuralEvalService neural, IBoardService boards) : BaseFeatureEvalServiceImpl(neural)
    {
        protected override IBoardService BoardService => boards;
        protected override EvalResultModel CalculateEvalModel(IBoardModel board, bool isWhite) => new();
    }

    private sealed class GatedScorer : INeuralEvalService
    {
        private readonly TaskCompletionSource[] _gates = Enumerable.Range(0, 6)
            .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();

        public Channel<int> Started { get; } = Channel.CreateUnbounded<int>();

        public Channel<int> Returned { get; } = Channel.CreateUnbounded<int>();

        public ConcurrentDictionary<int, IBoardModel> Boards { get; } = new();

        public int Active;

        public int Peak;

        public int Calls;

        public int MaxBatchSize => 32;

        public void Release(int key) => _gates[key].TrySetResult();

        public void Fail(int key, bool cancel)
        {
            if (cancel)
                _gates[key].TrySetCanceled();
            else
                _gates[key].TrySetException(new InvalidDataException("Controlled inference failure."));
        }

        public float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool isWhite)
        {
            Assert.False(isWhite);
            Assert.Equal(2, board.History.Events.Count);
            Assert.Equal(2, ((IDoublingCubeModel)board).DoublingCubeValue);
            var key = Enumerable.Range(1, 4).FirstOrDefault(index => board.Fields[index] == -1, 5);
            if (Boards.TryAdd(key, board))
                Started.Writer.TryWrite(key);
            return [key];
        }

        public async Task<float[][]> PredictBatchAsync(float[][] features)
        {
            Interlocked.Increment(ref Calls);
            var active = Interlocked.Increment(ref Active);
            int previous;
            do { previous = Peak; }
            while (active > previous && Interlocked.CompareExchange(ref Peak, active, previous) != previous);
            var key = (int)features[0][0];
            try
            {
                await _gates[key].Task;
                return features.Select(_ => new[] { 0.5f, 0f, 0f, 0f, 0f }).ToArray();
            }
            finally
            {
                Interlocked.Decrement(ref Active);
                Returned.Writer.TryWrite(key);
            }
        }

        public Task<float[]> PredictAsync(NormalizedEvalResultModel model, IBoardModel board, bool isWhite) => throw new InvalidOperationException("Two-ply must use batches.");
    }

    private sealed class FeatureScorer(GameModus modus) : INeuralEvalService
    {
        private readonly IFeatureVectorExtractor _extractor = FeatureVectorExtractorFactory.Create(modus);

        public ConcurrentBag<float[]> Rows { get; } = new();

        public int MaxBatchSize => 32;

        public float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool isWhite) => _extractor.Extract(model, board, isWhite);

        public async Task<float[][]> PredictBatchAsync(float[][] features)
        {
            await Task.Yield();
            return features.Select(row =>
            {
                Rows.Add(row);
                return new[] { (float)(0.5 + Math.Tanh(row.Sum(value => (double)value) / 100) / 4), 0f, 0f, 0f, 0f };
            }).ToArray();
        }

        public Task<float[]> PredictAsync(NormalizedEvalResultModel model, IBoardModel board, bool isWhite) => throw new InvalidOperationException("Two-ply must use batches.");
    }
}
