using GammonX.Engine.Extensions;
using GammonX.Engine.Models;
using GammonX.Engine.Services;
using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;
using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Tests.Services;

public class FeatureBufferTests
{
    public static IEnumerable<object[]> Variants() =>
        new[] { GameModus.Backgammon, GameModus.Tavla, GameModus.Portes, GameModus.Fevga, GameModus.Plakoto }
            .SelectMany(modus => new[] { true, false }.Select(white => new object[] { modus, white }));

    [Theory]
    [MemberData(nameof(Variants))]
    public async Task ReusesOnlyCompletedRowsAndKeepsConcurrentSearchesIndependent(GameModus modus, bool white)
    {
        var baseline = new Probe(modus);
        var reference = await Evaluate(CreateService(baseline, false));
        var probe = new Probe(modus) { Gate = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var sharedService = CreateService(probe, true);
        var first = Evaluate(sharedService);
        var second = Evaluate(sharedService);
        try
        {
            Assert.False(first.IsCompleted);
            Assert.False(second.IsCompleted);
            Assert.Equal(8, probe.Buffers.Count);
        }
        finally
        {
            probe.Gate.TrySetResult();
            await Task.WhenAll(first, second);
        }
        Assert.Equal(reference, await first);
        Assert.Equal(reference, await second);
        Assert.Equal(8, probe.Buffers.Count);
        Assert.True(probe.Reuses > 0);
        Assert.Equal(baseline.Rows * 2, probe.Rows);

        BaseFeatureEvalServiceImpl CreateService(Probe neural, bool reuse)
        {
            var service = (BaseFeatureEvalServiceImpl)FeatureEvalServiceFactory.Create(modus, neural);
            service.ReuseReplyBoard = service.BatchAcrossRolls = true;
            service.ReuseFeatureBuffers = reuse;
            return service;
        }
        async Task<double> Evaluate(BaseFeatureEvalServiceImpl service) =>
            (await service.EvalBoardStateAsync(new FeatureEvalOptions
            {
                EvalBoardRequest = new EvalBoardRequestContract
                {
                    TurnNumber = 0,
                    Board = BoardServiceFactory.Create(modus).CreateBoard().ToContract(false),
                    IsWhite = white, Modus = modus, BotLevel = BotLevel.Expert
                },
                ContactWeights = EvalWeights.DefaultContactWeights,
                TurnNumber = 0,
            })).Equity;
    }

    [Theory]
    [InlineData("pass", 21)]
    [InlineData("terminal", 0)]
    [InlineData("mixed", 5)]
    public async Task ReusePreservesPassAndTerminalRowMapping(string scenario, int expectedRows)
    {
        var reference = await Evaluate(false);
        Assert.Equal(reference, await Evaluate(true));

        async Task<double> Evaluate(bool reuse)
        {
            var probe = new Probe(GameModus.Backgammon);
            var service = (BaseFeatureEvalServiceImpl)FeatureEvalServiceFactory.Create(GameModus.Backgammon, probe);
            service.ReuseReplyBoard = service.BatchAcrossRolls = true;
            service.ReuseFeatureBuffers = reuse;
            var fields = new int[24];
            var board = new BoardModelContract { Fields = fields };
            if (scenario == "pass")
            {
                Array.Fill(fields, 2, 0, 6);
                board.HomeBarCountWhite = 1;
                board.BearOffCountWhite = 14;
                board.BearOffCountBlack = 3;
            }
            else
            {
                fields[scenario == "terminal" ? 0 : 5] = 1;
                fields[23] = -1;
                board.BearOffCountBlack = board.BearOffCountWhite = 14;
            }
            var score = await service.EvalBoardStateAsync(new FeatureEvalOptions
            {
                EvalBoardRequest = new EvalBoardRequestContract
                {
                    TurnNumber = 0,
                    Board = board, IsWhite = scenario != "pass",
                    Modus = GameModus.Backgammon, BotLevel = BotLevel.Expert
                },
                ContactWeights = EvalWeights.DefaultContactWeights,
                TurnNumber = 0,
            });
            Assert.Equal(expectedRows, probe.Rows);
            if (reuse) Assert.InRange(probe.Buffers.Count, 0, 4);
            return score.Equity;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedSearchBuffersAreNeverReusedByLaterSearches(bool cancel)
    {
        var probe = new Probe(GameModus.Backgammon) { Failure = cancel ? new OperationCanceledException() : new InvalidDataException() };
        var service = (BaseFeatureEvalServiceImpl)FeatureEvalServiceFactory.Create(GameModus.Backgammon, probe);
        service.ReuseReplyBoard = service.ReuseFeatureBuffers = true;
        Task<EvalResult> Evaluate() => service.EvalBoardStateAsync(new FeatureEvalOptions
        {
            EvalBoardRequest = new EvalBoardRequestContract
            {
                TurnNumber = 0,
                Board = BoardServiceFactory.Create(GameModus.Backgammon).CreateBoard().ToContract(false),
                IsWhite = true, Modus = GameModus.Backgammon, BotLevel = BotLevel.Expert
            },
            ContactWeights = EvalWeights.DefaultContactWeights,
            TurnNumber = 0,
        });
        Assert.NotNull(await Record.ExceptionAsync(Evaluate));
        var failedRows = probe.Buffers.Select(row => (Row: row, Copy: row.ToArray())).ToArray();
        probe.Failure = null;
        await Evaluate();
        Assert.Equal(8, probe.Buffers.Count);
        foreach (var row in failedRows) Assert.Equal(row.Copy, row.Row);
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void ExtractorsOverwriteEveryValueAndRejectWrongWidth(GameModus modus, bool white)
    {
        var board = BoardServiceFactory.Create(modus).CreateBoard();
        var extractor = FeatureVectorExtractorFactory.Create(modus);
        var model = new NormalizedEvalResultModel();
        var expected = extractor.Extract(model, board, white);
        var buffer = Enumerable.Repeat(float.NaN, extractor.FeatureCount).ToArray();
        extractor.Extract(model, board, white, buffer);
        Assert.Equal(expected, buffer);
        Array.Fill(buffer, float.PositiveInfinity);
        var changed = board.InvertBoard();
        extractor.Extract(model, changed, !white, buffer);
        Assert.Equal(extractor.Extract(model, changed, !white), buffer);
        Assert.Throws<ArgumentException>(() => extractor.Extract(model, board, white, new float[buffer.Length - 1]));
        Assert.Throws<ArgumentException>(() => extractor.Extract(model, board, white, new float[buffer.Length + 1]));
    }

    private sealed class Probe(GameModus modus) : INeuralEvalService
    {
        private readonly IFeatureVectorExtractor _extractor = FeatureVectorExtractorFactory.Create(modus);
        private readonly Lock _gate = new();
        public HashSet<float[]> Buffers { get; } = new(ReferenceEqualityComparer.Instance);
        public TaskCompletionSource? Gate;
        public Exception? Failure;
        public int Reuses;
        public int Rows;
        public int MaxBatchSize => 4;
        public float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool white) =>
            ExtractFeatures(model, board, white, null);
        public float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool white, float[]? destination)
        {
            lock (_gate)
            {
                if (destination != null) Reuses++;
                destination ??= new float[_extractor.FeatureCount];
                Buffers.Add(destination);
            }
            _extractor.Extract(model, board, white, destination);
            return destination;
        }
        public async Task<float[][]> PredictBatchAsync(float[][] features)
        {
            var copies = features.Select(row => row.ToArray()).ToArray();
            Assert.Equal(features.Length, features.Distinct(ReferenceEqualityComparer.Instance).Count());
            if (Gate != null) await Gate.Task;
            await Task.Yield();
            for (var row = 0; row < features.Length; row++) Assert.Equal(copies[row], features[row]);
            if (Failure != null) throw Failure;
            Interlocked.Add(ref Rows, features.Length);
            return features.Select(row => new[] { (float)(0.5 + Math.Tanh(row.Sum(value => (double)value) / 100) / 4), 0f, 0f, 0f, 0f }).ToArray();
        }
        public Task<float[]> PredictAsync(NormalizedEvalResultModel model, IBoardModel board, bool white) => throw new NotSupportedException();
    }
}
