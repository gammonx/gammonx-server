using GammonX.Engine.Extensions;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;
using GammonX.Mars.NN.Tests.Performance;
using GammonX.Mars.NN.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.NN.Tests.Services;

[Collection(nameof(PerformanceTestCollection))]
public class BatchEvaluationTests
{
    [Theory]
    [InlineData(GameModus.Backgammon)]
    [InlineData(GameModus.Portes)]
    [InlineData(GameModus.Tavla)]
    [InlineData(GameModus.Fevga)]
    [InlineData(GameModus.Plakoto)]
    public async Task NonHostedModelsImplementTheSameBatchContract(GameModus modus)
    {
        var path = Path.Combine("Data", "NeuralNets", modus.ToString(), "training_net.dat");
        var neural = NeuralEvalService.Load(modus, path, TorchSharp.torch.CPU);
        var eval = FeatureEvalServiceFactory.Create(modus, neural);
        var boardService = BoardServiceFactory.Create(modus);
        var rows = new List<float[]>();
        var expected = new List<float[]>();
        for (var turn = 0; turn < 3; turn++)
        {
            var board = boardService.CreateBoard();
            for (var history = 0; history < turn; history++)
                boardService.AddRollEventToHistory(board, true, [2, 3]);
            var model = eval.EvalPositionForTraining(board.ToContract(false), true);
            rows.Add(neural.ExtractFeatures(model, board, true));
            expected.Add(await neural.PredictAsync(model, board, true));
        }

        var predictions = await neural.PredictBatchAsync(rows.ToArray());

        Assert.Equal(expected.Count, predictions.Length);
        for (var row = 0; row < predictions.Length; row++)
        {
            Assert.Equal(5, predictions[row].Length);
            for (var head = 0; head < 5; head++)
                Assert.InRange(Math.Abs(predictions[row][head] - expected[row][head]), 0f, 1e-5f);
        }
        await Assert.ThrowsAsync<ArgumentException>(() => neural.PredictBatchAsync([]));
        await Assert.ThrowsAsync<ArgumentException>(() => neural.PredictBatchAsync(new float[neural.MaxBatchSize + 1][]));
        await Assert.ThrowsAsync<ArgumentException>(() => neural.PredictBatchAsync([[]]));
    }

    [Fact]
    public async Task ReplyGroupsStayBoundedAt32WhenWorkerLimitIsLarger()
    {
        var neural = new DeterministicNeuralService(GameModus.Backgammon, 64);
        var service = Assert.IsAssignableFrom<BaseFeatureEvalServiceImpl>(
            FeatureEvalServiceFactory.Create(GameModus.Backgammon, neural));
        var board = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PerformanceBoard1);
        Assert.NotNull(board);
        await service.EvalBoardStateAsync(new EvalBoardRequestContract
        {
            Board = board,
            IsWhite = false,
            Modus = GameModus.Backgammon,
            BotLevel = BotLevel.Expert
        }, EvalWeights.DefaultContactWeights);
        Assert.Equal(32, neural.LargestBatch);
    }

    [Theory]
    [InlineData(GameModus.Backgammon, 32)]
    [InlineData(GameModus.Portes, 32)]
    [InlineData(GameModus.Tavla, 32)]
    [InlineData(GameModus.Fevga, 32)]
    [InlineData(GameModus.Plakoto, 32)]
    [InlineData(GameModus.Backgammon, 64)]
    [InlineData(GameModus.Portes, 64)]
    [InlineData(GameModus.Tavla, 64)]
    [InlineData(GameModus.Fevga, 64)]
    [InlineData(GameModus.Plakoto, 64)]
    public async Task RealModelsMatchSingleRowReferenceScoring(GameModus modus, int maxBatchSize)
    {
        var path = Path.Combine("Data", "NeuralNets", modus.ToString(), "training_net.dat");
        using var neural = Assert.IsType<BatchedNeuralEvalService>(
            BatchedNeuralEvalService.Load(modus, path, TorchSharp.torch.CPU, maxBatchSize));
        await neural.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var service = Assert.IsAssignableFrom<BaseFeatureEvalServiceImpl>(
                FeatureEvalServiceFactory.Create(modus, neural));
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var request = new EvalBoardRequestContract
            {
                Board = board.ToContract(false),
                IsWhite = true,
                Modus = modus,
                BotLevel = BotLevel.Expert
            };
            var reference = await new TwoPlySearchEvaluator(boardService,
                async (positions, isWhite) =>
                {
                    var predictions = await neural.PredictAsync(
                        service.EvalPositionForTraining(positions[0].ToContract(false), isWhite), positions[0], isWhite);
                    return new[] { modus is GameModus.Fevga or GameModus.Plakoto
                        ? predictions[0]
                        : new GameEquityModel(new GameOutcomeModel(predictions)).Equity };
                }, maxBatchSize: 1).EvaluateAsync(board, true);
            var batched = await service.EvalBoardStateAsync(request, EvalWeights.DefaultContactWeights);
            Assert.True(double.IsFinite(reference));
            Assert.InRange(Math.Abs(reference - batched), 0d, 1e-5);
        }
        finally
        {
            await neural.StopAsync(CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(GameModus.Backgammon)]
    [InlineData(GameModus.Portes)]
    [InlineData(GameModus.Tavla)]
    [InlineData(GameModus.Fevga)]
    [InlineData(GameModus.Plakoto)]
    public async Task MatchesSinglePositionScoringAndPreservesHistoryFeatures(GameModus modus)
    {
        var boardService = BoardServiceFactory.Create(modus);
        var board = boardService.CreateBoard();
        for (var turn = 0; turn < 10; turn++)
            boardService.AddRollEventToHistory(board, turn % 2 == 0, [2, 3]);
        var originalHistory = board.History.Events.ToArray();
        var neural = new DeterministicNeuralService(modus);
        var eval = Assert.IsAssignableFrom<BaseFeatureEvalServiceImpl>(FeatureEvalServiceFactory.Create(modus, neural));
        var request = new EvalBoardRequestContract
        {
            Board = board.ToContract(false),
            IsWhite = true,
            Modus = modus,
            BotLevel = BotLevel.Expert
        };
        // Contracts omit history; evaluate the search itself with history-bearing copies.
        var legacyFeatures = new List<float[]>();
        async Task<double> Single(IBoardModel position, bool isWhite)
        {
            var features = FeatureVectorExtractorFactory.Create(modus).Extract(
                eval.EvalPositionForTraining(position.ToContract(false), isWhite), position, isWhite);
            legacyFeatures.Add(features);
            await Task.Yield();
            return features.Sum(value => (double)value);
        }
        var legacy = await new TwoPlySearchEvaluator(boardService,
            (positions, isWhite) => Task.WhenAll(positions.Select(position => Single(position, isWhite))),
            maxBatchSize: 1).EvaluateAsync(board, true);
        var batchFeatures = new List<float[]>();
        async Task<double[]> Batch(IReadOnlyList<IBoardModel> positions, bool isWhite)
        {
            await Task.Yield();
            return positions.Select(position =>
            {
                var features = FeatureVectorExtractorFactory.Create(modus).Extract(
                    eval.EvalPositionForTraining(position.ToContract(false), isWhite), position, isWhite);
                batchFeatures.Add(features);
                return features.Sum(value => (double)value);
            }).ToArray();
        }
        var batched = await new TwoPlySearchEvaluator(boardService, Batch, maxBatchSize: 4).EvaluateAsync(board, true);
        Assert.Equal(legacy, batched, 10);
        Assert.Equal(legacyFeatures.Count, batchFeatures.Count);
        for (var row = 0; row < legacyFeatures.Count; row++)
            Assert.Equal(legacyFeatures[row], batchFeatures[row]);
        Assert.Equal(originalHistory, board.History.Events.ToArray());

        var score = await eval.EvalBoardStateAsync(request, EvalWeights.DefaultContactWeights);
        Assert.True(double.IsFinite(score));
        Assert.True(neural.Predictions > 0);
        Assert.True(neural.Batches > 0);
    }

    [Fact]
    public async Task TerminalRepliesAreNotSentToBatchInference()
    {
        var fields = new int[24];
        fields[0] = 1;
        fields[23] = -1;
        var neural = new DeterministicNeuralService(GameModus.Backgammon);
        var eval = Assert.IsAssignableFrom<BaseFeatureEvalServiceImpl>(
            FeatureEvalServiceFactory.Create(GameModus.Backgammon, neural));
        var score = await eval.EvalBoardStateAsync(new EvalBoardRequestContract
        {
            Board = new BoardModelContract
            {
                Fields = fields,
                BearOffCountWhite = 14,
                BearOffCountBlack = 14
            },
            IsWhite = true,
            Modus = GameModus.Backgammon,
            BotLevel = BotLevel.Expert
        }, EvalWeights.DefaultContactWeights);
        Assert.Equal(-1d, score, 10);
        Assert.Equal(0, neural.Predictions);
    }

    [Fact]
    public async Task NoLegalOpponentReplyIsScoredAsASingleRowBatch()
    {
        var fields = new int[24];
        Array.Fill(fields, 2, 0, 6);
        var neural = new DeterministicNeuralService(GameModus.Backgammon);
        var eval = Assert.IsAssignableFrom<BaseFeatureEvalServiceImpl>(
            FeatureEvalServiceFactory.Create(GameModus.Backgammon, neural));

        var score = await eval.EvalBoardStateAsync(new EvalBoardRequestContract
        {
            Board = new BoardModelContract
            {
                Fields = fields,
                HomeBarCountWhite = 1
            },
            IsWhite = false,
            Modus = GameModus.Backgammon,
            BotLevel = BotLevel.Expert
        }, EvalWeights.DefaultContactWeights);

        Assert.True(double.IsFinite(score));
        Assert.Equal(21, neural.Predictions);
        Assert.Equal(21, neural.Batches);
    }

    [Fact]
    public async Task BatchScorerFailureRestoresRepliesAndInputAfterAwait()
    {
        var boardService = BoardServiceFactory.Create(GameModus.Backgammon);
        var board = boardService.CreateBoard();
        var original = board.DeepClone();
        var replies = new List<IBoardModel>();
        var evaluator = new TwoPlySearchEvaluator(boardService, async (positions, _) =>
            {
                replies.AddRange(positions);
                await Task.Yield();
                throw new InvalidDataException("Expected scorer failure.");
            }, maxBatchSize: 4);

        await Assert.ThrowsAsync<InvalidDataException>(() => evaluator.EvaluateAsync(board, true));

        Assert.Equal(4, replies.Count);
        Assert.All(replies, reply =>
        {
            Assert.True(reply.Equals(original));
            Assert.Empty(reply.History.Events);
        });
        Assert.True(board.Equals(original));
        Assert.Empty(board.History.Events);
    }

    [Fact]
    public async Task BatchResultCountMismatchIsAnErrorAndRestoresBoards()
    {
        var boardService = BoardServiceFactory.Create(GameModus.Backgammon);
        var board = boardService.CreateBoard();
        var original = board.DeepClone();
        var evaluator = new TwoPlySearchEvaluator(boardService,
            (_, _) => Task.FromResult(Array.Empty<double>()));
        await Assert.ThrowsAsync<InvalidDataException>(() => evaluator.EvaluateAsync(board, true));
        Assert.True(board.Equals(original));
    }

    private sealed class DeterministicNeuralService(GameModus modus, int maxBatchSize = 4) : INeuralEvalService
    {
        private readonly IFeatureVectorExtractor _extractor = FeatureVectorExtractorFactory.Create(modus);
        public int Predictions { get; private set; }
        public int Batches { get; private set; }
        public int LargestBatch { get; private set; }
        public int MaxBatchSize => maxBatchSize;
        public float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool isWhite) =>
            _extractor.Extract(model, board, isWhite);

        public Task<float[]> PredictAsync(NormalizedEvalResultModel model, IBoardModel board, bool isWhite)
        {
            Predictions++;
            return Task.FromResult(Predict(ExtractFeatures(model, board, isWhite)));
        }

        public async Task<float[][]> PredictBatchAsync(float[][] features)
        {
            Assert.InRange(features.Length, 1, MaxBatchSize);
            LargestBatch = Math.Max(LargestBatch, features.Length);
            Batches++;
            Predictions += features.Length;
            await Task.Yield();
            return features.Select(Predict).ToArray();
        }

        private static float[] Predict(float[] features)
        {
            var value = (float)(0.5 + Math.Tanh(features.Sum(value => (double)value) / 100) / 4);
            return [value, 0f, 0f, 0f, 0f];
        }
    }
}
