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
using Moq;

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
            var model = eval.EvalPositionForTraining(new FeatureEvalOptions
            {
                BoardContract = board.ToContract(false),
                IsWhite = true,
                TurnNumber = turn,
            });
            rows.Add(neural.ExtractFeatures(model, board, true));
            var destination = Enumerable.Repeat(float.NaN, rows[^1].Length).ToArray();
            Assert.Same(destination, neural.ExtractFeatures(model, board, true, destination));
            Assert.Equal(rows[^1], destination);
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
        service.ReuseReplyBoard = true;
        var board = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PerformanceBoard1);
        Assert.NotNull(board);
        await service.EvalBoardStateAsync(new FeatureEvalOptions
        {
            EvalBoardRequest = new EvalBoardRequestContract
            {
                TurnNumber = 0,
                Board = board,
                IsWhite = false,
                Modus = GameModus.Backgammon,
                BotLevel = BotLevel.Expert
            },
            ContactWeights = EvalWeights.DefaultContactWeights,
            TurnNumber = 0,
        });
        Assert.Equal(32, neural.LargestBatch);
    }

    [Theory]
    [InlineData(GameModus.Backgammon, 32, true)]
    [InlineData(GameModus.Portes, 32, true)]
    [InlineData(GameModus.Tavla, 32, true)]
    [InlineData(GameModus.Fevga, 32, true)]
    [InlineData(GameModus.Plakoto, 32, true)]
    [InlineData(GameModus.Backgammon, 64, true)]
    [InlineData(GameModus.Portes, 64, true)]
    [InlineData(GameModus.Tavla, 64, true)]
    [InlineData(GameModus.Fevga, 64, true)]
    [InlineData(GameModus.Plakoto, 64, true)]
    [InlineData(GameModus.Backgammon, 32, false)]
    [InlineData(GameModus.Portes, 32, false)]
    [InlineData(GameModus.Tavla, 32, false)]
    [InlineData(GameModus.Fevga, 32, false)]
    [InlineData(GameModus.Plakoto, 32, false)]
    [InlineData(GameModus.Backgammon, 64, false)]
    [InlineData(GameModus.Portes, 64, false)]
    [InlineData(GameModus.Tavla, 64, false)]
    [InlineData(GameModus.Fevga, 64, false)]
    [InlineData(GameModus.Plakoto, 64, false)]
    public async Task RealModelsMatchSingleRowReferenceScoring(GameModus modus, int maxBatchSize, bool reuseReplyBoard)
    {
        var path = Path.Combine("Data", "NeuralNets", modus.ToString(), "training_net.dat");
        using var neural = Assert.IsType<BatchedNeuralEvalService>(
            BatchedNeuralEvalService.Load(modus, path, TorchSharp.torch.CPU, maxBatchSize));
        await neural.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var service = Assert.IsAssignableFrom<BaseFeatureEvalServiceImpl>(
                FeatureEvalServiceFactory.Create(modus, neural));
            service.BatchAcrossRolls = true;
            service.ReuseReplyBoard = reuseReplyBoard;
            service.ReuseFeatureBuffers = true;
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var request = new EvalBoardRequestContract
            {
                TurnNumber = 37,
                Board = board.ToContract(false),
                IsWhite = true,
                Modus = modus,
                BotLevel = BotLevel.Expert
            };
            var reference = await new TwoPlySearchEvaluator(boardService,
                async (positions, isWhite) =>
                {
                    var predictions = await neural.PredictAsync(
                        service.EvalPositionForTraining(new FeatureEvalOptions
                        {
                            BoardContract = positions[0].ToContract(false),
                            IsWhite = isWhite,
                            // The scored reply belongs to the opponent's next turn.
                            TurnNumber = request.TurnNumber + 1,
                        }), positions[0], isWhite);
                    return new[] { new EvalResult(modus is GameModus.Fevga or GameModus.Plakoto
                        ? predictions[0]
                        : new GameEquityModel(new GameOutcomeModel(predictions)).Equity, predictions) };
                }, maxBatchSize: 1).EvaluateAsync(board, true);
            var batched = await service.EvalBoardStateAsync(new FeatureEvalOptions
            {
                EvalBoardRequest = request,
                ContactWeights = EvalWeights.DefaultContactWeights,
                TurnNumber = request.TurnNumber,
            });
            Assert.True(double.IsFinite(reference.Equity));
            Assert.InRange(Math.Abs(reference.Equity - batched.Equity), 0d, 1e-5);
            Assert.NotNull(reference.Prediction);
            Assert.NotNull(batched.Prediction);
            Assert.Equal(reference.Prediction.Length, batched.Prediction.Length);
            for (var head = 0; head < reference.Prediction.Length; head++)
                Assert.InRange(Math.Abs(reference.Prediction[head] - batched.Prediction[head]), 0f, 1e-5f);
        }
        finally
        {
            await neural.StopAsync(CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(GameModus.Backgammon, true)]
    [InlineData(GameModus.Backgammon, false)]
    [InlineData(GameModus.Portes, true)]
    [InlineData(GameModus.Portes, false)]
    [InlineData(GameModus.Tavla, true)]
    [InlineData(GameModus.Tavla, false)]
    [InlineData(GameModus.Fevga, true)]
    [InlineData(GameModus.Fevga, false)]
    [InlineData(GameModus.Plakoto, true)]
    [InlineData(GameModus.Plakoto, false)]
    public async Task MatchesSinglePositionScoringAndPreservesHistoryFeatures(GameModus modus, bool rootIsWhite)
    {
        var boardService = BoardServiceFactory.Create(modus);
        var board = boardService.CreateBoard();
        for (var turn = 0; turn < 10; turn++)
            boardService.AddRollEventToHistory(board, turn % 2 == 0, [2, 3]);
        var originalHistory = board.History.Events.ToArray();
        var neural = new DeterministicNeuralService(modus);
        var eval = Assert.IsAssignableFrom<BaseFeatureEvalServiceImpl>(FeatureEvalServiceFactory.Create(modus, neural));
        eval.ReuseReplyBoard = true;
        var request = new EvalBoardRequestContract
        {
            TurnNumber = 0,
            Board = board.ToContract(false),
            IsWhite = rootIsWhite,
            Modus = modus,
            BotLevel = BotLevel.Expert
        };
        // Contracts omit history; evaluate the search itself with history-bearing copies.
        var legacyFeatures = new List<float[]>();
        async Task<EvalResult> Single(IBoardModel position, bool isWhite)
        {
            var features = FeatureVectorExtractorFactory.Create(modus).Extract(
                eval.EvalPositionForTraining(new FeatureEvalOptions
                {
                    BoardContract = position.ToContract(false),
                    IsWhite = isWhite,
                    TurnNumber = 0,
                }), position, isWhite);
            legacyFeatures.Add(features);
            await Task.Yield();
            return new EvalResult(features.Sum(value => (double)value), null);
        }
        var legacy = await new TwoPlySearchEvaluator(boardService,
            (positions, isWhite) => Task.WhenAll(positions.Select(position => Single(position, isWhite))),
            maxBatchSize: 1, batchAcrossRolls: false).EvaluateAsync(board, rootIsWhite);
        var batchFeatures = new List<float[]>();
        async Task<EvalResult[]> Batch(IReadOnlyList<IBoardModel> positions, bool isWhite)
        {
            await Task.Yield();
            return positions.Select(position =>
            {
                var features = FeatureVectorExtractorFactory.Create(modus).Extract(
                    eval.EvalPositionForTraining(new FeatureEvalOptions
                    {
                        BoardContract = position.ToContract(false),
                        IsWhite = isWhite,
                        TurnNumber = 0,
                    }), position, isWhite);
                batchFeatures.Add(features);
                return new EvalResult(features.Sum(value => (double)value), null);
            }).ToArray();
        }
        var batched = await new TwoPlySearchEvaluator(boardService, Batch, maxBatchSize: 4, batchAcrossRolls: true).EvaluateAsync(board, rootIsWhite);
        Assert.Equal(legacy.Equity, batched.Equity, 10);
        Assert.Equal(legacyFeatures.Count, batchFeatures.Count);
        for (var row = 0; row < legacyFeatures.Count; row++)
            Assert.Equal(legacyFeatures[row], batchFeatures[row]);
        Assert.Equal(originalHistory, board.History.Events.ToArray());

        IBoardModel? working = null;
        var preparedFeatures = new List<float[]>();
        var preparedEvaluator = new TwoPlySearchEvaluator(boardService, Batch,
            position => working = position.DeepClone(), maxBatchSize: 4, batchAcrossRolls: true);
        var preparedScore = await preparedEvaluator.EvalPreparedAsync(board, rootIsWhite,
            (position, white) =>
            {
                var features = FeatureVectorExtractorFactory.Create(modus).Extract(
                    eval.EvalPositionForTraining(new FeatureEvalOptions
                    {
                        BoardContract = position.ToContract(false),
                        IsWhite = white,
                        TurnNumber = 0,
                    }), position, white);
                preparedFeatures.Add(features);
                return new PreparedPosition(features);
            }, async positions =>
            {
                await Task.Yield();
                Assert.NotNull(working);
                Assert.True(working.Equals(board));
                Assert.Equal(JsonConvert.SerializeObject(board.History), JsonConvert.SerializeObject(working.History));
                return positions.Select(position => new EvalResult(position.Features!.Sum(value => (double)value), null)).ToArray();
            });
        Assert.Equal(BitConverter.DoubleToInt64Bits(legacy.Equity), BitConverter.DoubleToInt64Bits(preparedScore.Equity));
        Assert.Equal(legacyFeatures.Count, preparedFeatures.Count);
        for (var row = 0; row < legacyFeatures.Count; row++)
            Assert.Equal(legacyFeatures[row], preparedFeatures[row]);

        var score = await eval.EvalBoardStateAsync(new FeatureEvalOptions
        {
            EvalBoardRequest = request,
            ContactWeights = EvalWeights.DefaultContactWeights,
            TurnNumber = request.TurnNumber,
        });
        Assert.True(double.IsFinite(score.Equity));
        Assert.True(neural.Predictions > 0);
        Assert.True(neural.Batches > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TerminalRepliesAreNotSentToBatchInference(bool reuseReplyBoard)
    {
        var fields = new int[24];
        fields[0] = 1;
        fields[23] = -1;
        var neural = new DeterministicNeuralService(GameModus.Backgammon);
        var eval = Assert.IsAssignableFrom<BaseFeatureEvalServiceImpl>(
            FeatureEvalServiceFactory.Create(GameModus.Backgammon, neural));
        eval.ReuseReplyBoard = reuseReplyBoard;
        var score = await eval.EvalBoardStateAsync(new FeatureEvalOptions
        {
            EvalBoardRequest = new EvalBoardRequestContract
            {
                TurnNumber = 0,
                Board = new BoardModelContract
                {
                    Fields = fields,
                    BearOffCountWhite = 14,
                    BearOffCountBlack = 14
                },
                IsWhite = true,
                Modus = GameModus.Backgammon,
                BotLevel = BotLevel.Expert
            },
            ContactWeights = EvalWeights.DefaultContactWeights,
            TurnNumber = 0,
        });
        Assert.Equal(-1d, score.Equity, 10);
        Assert.Equal(0, neural.Predictions);
    }

    [Theory]
    [InlineData(false, false, 21)]
    [InlineData(false, true, 21)]
    [InlineData(true, false, 6)]
    [InlineData(true, true, 6)]
    public async Task NoLegalOpponentReplyContributesOneRowPerRollToBoundedBatches(bool batchAcrossRolls, bool reuseReplyBoard, int expectedBatches)
    {
        var fields = new int[24];
        Array.Fill(fields, 2, 0, 6);
        var neural = new DeterministicNeuralService(GameModus.Backgammon);
        var eval = Assert.IsAssignableFrom<BaseFeatureEvalServiceImpl>(
            FeatureEvalServiceFactory.Create(GameModus.Backgammon, neural));
        eval.BatchAcrossRolls = batchAcrossRolls;
        eval.ReuseReplyBoard = reuseReplyBoard;

        var score = await eval.EvalBoardStateAsync(new FeatureEvalOptions
        {
            EvalBoardRequest = new EvalBoardRequestContract
            {
                TurnNumber = 0,
                Board = new BoardModelContract
                {
                    Fields = fields,
                    HomeBarCountWhite = 1,
                    BearOffCountWhite = 14,
                    BearOffCountBlack = 3
                },
                IsWhite = false,
                Modus = GameModus.Backgammon,
                BotLevel = BotLevel.Expert
            },
            ContactWeights = EvalWeights.DefaultContactWeights,
            TurnNumber = 0,
        });

        Assert.True(double.IsFinite(score.Equity));
        Assert.Equal(21, neural.Predictions);
        Assert.Equal(expectedBatches, neural.Batches);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task MixedTerminalAndNeuralRepliesKeepTheirRollScores(bool batchAcrossRolls, bool reuseReplyBoard)
    {
        var fields = new int[24];
        fields[5] = 1;
        fields[23] = -1;
        var sizes = new List<int>();
        var neural = new Mock<INeuralEvalService>(MockBehavior.Strict);
        neural.SetupGet(service => service.MaxBatchSize).Returns(4);
        neural.Setup(service => service.ExtractFeatures(
                It.IsAny<NormalizedEvalResultModel>(), It.IsAny<IBoardModel>(), It.IsAny<bool>()))
            .Returns((NormalizedEvalResultModel _, IBoardModel board, bool isWhite) => Extract(board, isWhite, null));
        neural.Setup(service => service.ExtractFeatures(
                It.IsAny<NormalizedEvalResultModel>(), It.IsAny<IBoardModel>(), It.IsAny<bool>(), It.IsAny<float[]?>()))
            .Returns((NormalizedEvalResultModel _, IBoardModel board, bool isWhite, float[]? destination) => Extract(board, isWhite, destination));
        static float[] Extract(IBoardModel board, bool isWhite, float[]? destination)
        {
            Assert.False(isWhite);
            Assert.Equal(14, board.BearOffCountBlack);
            destination ??= new float[1];
            Assert.Single(destination);
            destination[0] = 0;
            return destination;
        }
        neural.Setup(service => service.PredictBatchAsync(It.IsAny<float[][]>()))
            .Returns(async (float[][] rows) =>
            {
                sizes.Add(rows.Length);
                await Task.Yield();
                return rows.Select(_ => new[] { 0.5f, 0f, 0f, 0f, 0f }).ToArray();
            });
        var eval = Assert.IsAssignableFrom<BaseFeatureEvalServiceImpl>(
            FeatureEvalServiceFactory.Create(GameModus.Backgammon, neural.Object));
        eval.BatchAcrossRolls = batchAcrossRolls;
        eval.ReuseReplyBoard = reuseReplyBoard;

        var score = await eval.EvalBoardStateAsync(new FeatureEvalOptions
        {
            EvalBoardRequest = new EvalBoardRequestContract
            {
                TurnNumber = 0,
                Board = new BoardModelContract
                {
                    Fields = fields, BearOffCountWhite = 14, BearOffCountBlack = 14
                },
                IsWhite = true, Modus = GameModus.Backgammon, BotLevel = BotLevel.Expert
            },
            ContactWeights = EvalWeights.DefaultContactWeights,
            TurnNumber = 0,
        });

        // Only 11, 12, 13, 14 and 23 fail to bear off: nine of 36 equally likely rolls.
        // Their neural equity is zero; all other rolls win a single for the opponent.
        Assert.Equal(-27d / 36d, score.Equity, 12);
        Assert.Equal(batchAcrossRolls ? new[] { 4, 1 } : new[] { 1, 1, 1, 1, 1 }, sizes);
        Assert.Equal(1, fields[5]);
        Assert.Equal(-1, fields[23]);
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
            (_, _) => Task.FromResult(Array.Empty<EvalResult>()));
        await Assert.ThrowsAsync<InvalidDataException>(() => evaluator.EvaluateAsync(board, true));
        Assert.True(board.Equals(original));
    }

    [Theory]
    [InlineData("hit", true)]
    [InlineData("bar", true)]
    [InlineData("bearoff", true)]
    [InlineData("pins", true)]
    [InlineData("pins", false)]
    public async Task PreparedRepliesPreserveSpecialBoardState(string scenario, bool rootIsWhite)
    {
        var modus = scenario == "pins" ? GameModus.Plakoto : GameModus.Backgammon;
        var boards = BoardServiceFactory.Create(modus);
        var fields = new int[24];
        var contract = new BoardModelContract { Fields = fields, DoublingCubeValue = 2 };
        switch (scenario)
        {
            case "hit": fields[0] = -14; fields[22] = -1; fields[23] = 15; break;
            case "bar":
                fields[0] = -4; fields[23] = -1; fields[10] = 14;
                for (var index = 18; index < 23; index++) fields[index] = -2;
                contract.HomeBarCountBlack = 1;
                break;
            case "bearoff": fields[5] = 15; fields[23] = -15; break;
            case "pins":
                fields[6] = -2; fields[8] = 1; fields[12] = 2;
                contract.PinnedFields = new int[24]; contract.PinnedFields[12] = -1;
                contract.BearOffCountWhite = contract.BearOffCountBlack = 12;
                break;
        }
        var board = boards.CreateBoard(contract);
        boards.AddRollEventToHistory(board, true, [2, 3]);
        var original = Snapshot(board);
        var expected = new List<string>();
        var evaluator = new TwoPlySearchEvaluator(boards, (positions, _) =>
        {
            expected.AddRange(positions.Select(Snapshot));
            return Task.FromResult(new EvalResult[positions.Count]);
        }, maxBatchSize: 4, batchAcrossRolls: true);
        await evaluator.EvaluateAsync(board, rootIsWhite);
        var actual = new List<string>();
        IBoardModel? working = null;
        evaluator = new TwoPlySearchEvaluator(boards, (_, _) => throw new Exception(),
            position => working = position.DeepClone(), 4, true);
        await evaluator.EvalPreparedAsync(board, rootIsWhite, (position, _) =>
        {
            actual.Add(Snapshot(position));
            return new PreparedPosition([0]);
        }, async positions =>
        {
            await Task.Yield();
            Assert.Equal(original, Snapshot(working!));
            return new EvalResult[positions.Count];
        });
        Assert.NotEmpty(expected);
        Assert.Equal(expected, actual);
        Assert.Equal(original, Snapshot(board));

        static string Snapshot(IBoardModel position) => JsonConvert.SerializeObject(new
        {
            Board = position.ToContract(false), History = position.History
        });
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

        public float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool isWhite, float[]? destination)
        {
            destination ??= new float[_extractor.FeatureCount];
            _extractor.Extract(model, board, isWhite, destination);
            return destination;
        }

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
