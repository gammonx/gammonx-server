using GammonX.Mars.Server.Contracts;
using GammonX.Mars.Server.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Microsoft.AspNetCore.Mvc.Testing;

using Newtonsoft.Json;

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Net.Http.Json;

using GammonX.Engine.Diagnostics;

using static TorchSharp.torch;

namespace GammonX.Mars.Server.Tests.Controller
{
    public class EvalControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public EvalControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(_ =>
            {
                // pass
            });
        }

        [Fact]
        public async Task MultipleHostsUseConfiguredInferenceThreads()
        {
            var threadCounts = new ConcurrentQueue<(int IntraOp, int InterOp)>();
            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == EvaluationMetrics.MeterName && instrument.Name == EvaluationMetrics.BatchSizeName)
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<long>((_, _, _, _) =>
                threadCounts.Enqueue((get_num_threads(), get_num_interop_threads())));
            listener.Start();

            using var secondFactory = _factory.WithWebHostBuilder(_ => { });
            using var firstClient = _factory.CreateClient();
            using var secondClient = secondFactory.CreateClient();
            var board = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.DefaultBoard1);
            Assert.NotNull(board);
            var request = new EvalBoardRequestContract
            {
                TurnNumber = 0,
                Board = board,
                Modus = GameModus.Backgammon,
                IsWhite = true,
                BotLevel = BotLevel.Hard
            };

            foreach (var client in new[] { firstClient, secondClient })
            {
                using var response = await client.PostAsJsonAsync("/bot/mars/api/eval/board", request, TestContext.Current.CancellationToken);
                response.EnsureSuccessStatusCode();
            }

            Assert.True(threadCounts.Count >= 2);
            var expectedIntraOp = int.Parse(Environment.GetEnvironmentVariable("MKL_NUM_THREADS")
                ?? Environment.GetEnvironmentVariable("OMP_NUM_THREADS") ?? "1");
            var expectedInterOp = int.Parse(Environment.GetEnvironmentVariable("TORCH_INTEROP_THREADS") ?? "1");
            Assert.All(threadCounts, counts => Assert.Equal((expectedIntraOp, expectedInterOp), counts));
        }

        #region Board Eval

        [Theory]
        [InlineData(GameModus.Plakoto, true)]
        [InlineData(GameModus.Plakoto, false)]
        public async Task CanEvalPlakotoBoardStateAsync(GameModus modus, bool isWhite)
        {
            var client = _factory.CreateClient();

            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PlakotoBoard1);
            Assert.NotNull(boardContract);

            var boardStateRequest = new EvalBoardRequestContract()
            {
                TurnNumber = 0,
                Board = boardContract,
                Modus = modus,
                IsWhite = isWhite
            };

            var response = await client.PostAsJsonAsync("/bot/mars/api/eval/board", boardStateRequest, TestContext.Current.CancellationToken);
            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var boardEval = JsonConvert.DeserializeObject<ResponseContract<BoardEvalPayload>>(json);

            Assert.NotNull(boardEval);
            Assert.Equal("OK", boardEval.Type);
            Assert.IsType<BoardEvalPayload>(boardEval.Payload);
            // Legacy pWin is now expressed as cubeless equity: 2 * pWin - 1.
            Assert.InRange(boardEval.Payload.EvalScore, -0.2, 0.0);
        }

        [Theory]
        [InlineData(GameModus.Portes, true)]
        [InlineData(GameModus.Backgammon, true)]
        [InlineData(GameModus.Tavla, true)]
        [InlineData(GameModus.Portes, false)]
        [InlineData(GameModus.Backgammon, false)]
        [InlineData(GameModus.Tavla, false)]
        public async Task CanEvalDefaultBoardStateAsync(GameModus modus, bool isWhite)
        {
            var client = _factory.CreateClient();

            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.DefaultBoard1);
            Assert.NotNull(boardContract);

            var boardStateRequest = new EvalBoardRequestContract()
            {
                TurnNumber = 0,
                Board = boardContract,
                Modus = modus,
                IsWhite = isWhite,
                BotLevel = BotLevel.Hard
            };

            var response = await client.PostAsJsonAsync("/bot/mars/api/eval/board", boardStateRequest, TestContext.Current.CancellationToken);
            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var boardEval = JsonConvert.DeserializeObject<ResponseContract<BoardEvalPayload>>(json);

            Assert.NotNull(boardEval);
            Assert.Equal("OK", boardEval.Type);
            Assert.IsType<BoardEvalPayload>(boardEval.Payload);
            if (!isWhite)
            {
                // black has stronger board, almost guaranteed win
                Assert.True(boardEval.Payload.EvalScore > 0.45);
            }
            else
            {
                Assert.True(boardEval.Payload.EvalScore < 0);
            }
        }

        [Theory]
        [InlineData(GameModus.Fevga, true)]
        [InlineData(GameModus.Fevga, false)]
        public async Task CanEvalFevgaBoardStateAsync(GameModus modus, bool isWhite)
        {
            var client = _factory.CreateClient();

            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.FevgaBoard1);
            Assert.NotNull(boardContract);

            var boardStateRequest = new EvalBoardRequestContract()
            {
                TurnNumber = 0,
                Board = boardContract,
                Modus = modus,
                IsWhite = isWhite
            };

            var response = await client.PostAsJsonAsync("/bot/mars/api/eval/board", boardStateRequest, TestContext.Current.CancellationToken);
            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var boardEval = JsonConvert.DeserializeObject<ResponseContract<BoardEvalPayload>>(json);

            Assert.NotNull(boardEval);
            Assert.Equal("OK", boardEval.Type);
            Assert.IsType<BoardEvalPayload>(boardEval.Payload);
            if (!isWhite)
            {
                // black has stronger board
                Assert.InRange(boardEval.Payload.EvalScore, -0.2, 0.4);
            }
            else
            {
                Assert.InRange(boardEval.Payload.EvalScore, -0.6, -0.1);
            }
        }

        #endregion Board Eval

        #region Move Eval

        [Theory]
        [InlineData(GameModus.Plakoto, true)]
        [InlineData(GameModus.Plakoto, false)]
        public async Task CanEvalPlakotoMovesAsync(GameModus modus, bool isWhite)
        {
            var client = _factory.CreateClient();

            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PlakotoBoard1);
            Assert.NotNull(boardContract);

            var moveRequest = new EvalMoveRequestContract()
            {
                TurnNumber = 0,
                Board = boardContract,
                Modus = modus,
                IsWhite = isWhite,
                Rolls = new[] { 2, 1 },
                BotLevel = BotLevel.Hard
            };

            var response = await client.PostAsJsonAsync("/bot/mars/api/eval/move", moveRequest, TestContext.Current.CancellationToken);
            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var moveEval = JsonConvert.DeserializeObject<ResponseContract<MoveEvalPayload>>(json);

            Assert.NotNull(moveEval);
            Assert.Equal("OK", moveEval.Type);
            Assert.IsType<MoveEvalPayload>(moveEval.Payload);
            Assert.NotNull(moveEval.Payload.MoveSequence);
            Assert.Equal(2, moveEval.Payload.MoveSequence.Moves.Count);
        }

        [Theory]
        [InlineData(GameModus.Portes, true)]
        [InlineData(GameModus.Backgammon, true)]
        [InlineData(GameModus.Tavla, true)]
        [InlineData(GameModus.Portes, false)]
        [InlineData(GameModus.Backgammon, false)]
        [InlineData(GameModus.Tavla, false)]
        public async Task CanEvalDefaultMovesAsync(GameModus modus, bool isWhite)
        {
            var client = _factory.CreateClient();

            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.DefaultBoard1);
            Assert.NotNull(boardContract);

            var moveRequest = new EvalMoveRequestContract()
            {
                TurnNumber = 0,
                Board = boardContract,
                Modus = modus,
                IsWhite = isWhite,
                Rolls = new[] { 2, 1 },
                BotLevel = BotLevel.Hard
            };

            var response = await client.PostAsJsonAsync("/bot/mars/api/eval/move", moveRequest, TestContext.Current.CancellationToken);
            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var moveEval = JsonConvert.DeserializeObject<ResponseContract<MoveEvalPayload>>(json);

            Assert.NotNull(moveEval);
            Assert.Equal("OK", moveEval.Type);
            Assert.IsType<MoveEvalPayload>(moveEval.Payload);
            Assert.NotNull(moveEval.Payload.MoveSequence);
            Assert.Equal(2, moveEval.Payload.MoveSequence.Moves.Count);
        }

        [Theory]
        [InlineData(GameModus.Fevga, true)]
        [InlineData(GameModus.Fevga, false)]
        public async Task CanEvalFevgaMovesAsync(GameModus modus, bool isWhite)
        {
            var client = _factory.CreateClient();

            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.FevgaBoard1);
            Assert.NotNull(boardContract);

            var moveRequest = new EvalMoveRequestContract()
            {
                TurnNumber = 0,
                Board = boardContract,
                Modus = modus,
                IsWhite = isWhite,
                Rolls = new[] { 2, 1 },
                BotLevel = BotLevel.Hard
            };

            var response = await client.PostAsJsonAsync("/bot/mars/api/eval/move", moveRequest, TestContext.Current.CancellationToken);
            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var moveEval = JsonConvert.DeserializeObject<ResponseContract<MoveEvalPayload>>(json);

            Assert.NotNull(moveEval);
            Assert.Equal("OK", moveEval.Type);
            Assert.IsType<MoveEvalPayload>(moveEval.Payload);
            Assert.Equal(2, moveEval.Payload.MoveSequence.Moves.Count);
        }

        #endregion Move Eval

        #region Cube Eval

        [Theory]
        [InlineData(GameModus.Backgammon, true)]
        [InlineData(GameModus.Backgammon, false)]
        public async Task CanEvalBackgammonCubeAsync(GameModus modus, bool isWhite)
        {
            var client = _factory.CreateClient();

            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.DefaultBoard1);
            Assert.NotNull(boardContract);

            var cubeRequest = new EvalCubeRequestContract()
            {
                TurnNumber = 0,
                Board = boardContract,
                Modus = modus,
                IsWhite = isWhite,
                MatchLength = 1,
                PointsAwayOpp = 1,
                PointsAwayPlayer = 1,
                BotLevel = BotLevel.Hard
            };

            var response = await client.PostAsJsonAsync("/bot/mars/api/eval/cube", cubeRequest, TestContext.Current.CancellationToken);
            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var cubeEval = JsonConvert.DeserializeObject<ResponseContract<CubeEvalPayload>>(json);

            Assert.NotNull(cubeEval);
            Assert.Equal("OK", cubeEval.Type);
            Assert.IsType<CubeEvalPayload>(cubeEval.Payload);
            Assert.Equal(CubeAction.NoDouble, cubeEval.Payload.ShouldOffer);
            Assert.Equal(CubeAction.Take, cubeEval.Payload.ShouldTake);
        }

        [Theory]
        [InlineData(GameModus.Portes, true)]
        [InlineData(GameModus.Portes, false)]
        [InlineData(GameModus.Tavla, true)]
        [InlineData(GameModus.Tavla, false)]
        [InlineData(GameModus.Fevga, true)]
        [InlineData(GameModus.Fevga, false)]
        [InlineData(GameModus.Plakoto, true)]
        [InlineData(GameModus.Plakoto, false)]
        public async Task CannotEvalCubeAsync(GameModus modus, bool isWhite)
        {
            var client = _factory.CreateClient();

            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.DefaultBoard1);
            Assert.NotNull(boardContract);

            var cubeRequest = new EvalCubeRequestContract()
            {
                TurnNumber = 0,
                Board = boardContract,
                Modus = modus,
                IsWhite = isWhite,
                MatchLength = 1,
                PointsAwayOpp = 1,
                PointsAwayPlayer = 1,
                BotLevel = BotLevel.Hard
            };

            var response = await client.PostAsJsonAsync("/bot/mars/api/eval/cube", cubeRequest, TestContext.Current.CancellationToken);
            var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var cubeEval = JsonConvert.DeserializeObject<ResponseContract<RequestErrorPayload>>(json);

            Assert.NotNull(cubeEval);
            Assert.Equal("ERROR", cubeEval.Type);
            Assert.IsType<RequestErrorPayload>(cubeEval.Payload);
            Assert.Equal("CUBE_EVAL_ERROR", cubeEval.Payload.Code);
            Assert.NotEmpty(cubeEval.Payload.Message);
        }

        #endregion Cube Eval
    }
}
