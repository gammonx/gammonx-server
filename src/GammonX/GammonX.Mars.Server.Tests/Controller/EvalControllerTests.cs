using GammonX.Mars.Server.Contracts;
using GammonX.Mars.Server.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Microsoft.AspNetCore.Mvc.Testing;

using Newtonsoft.Json;

using System.Net.Http.Json;

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
                Board = boardContract,
                Modus = modus,
                IsWhite = isWhite
            };

            var response = await client.PostAsJsonAsync("/bot/mars/api/eval/board", boardStateRequest);
            var json = await response.Content.ReadAsStringAsync();
            var boardEval = JsonConvert.DeserializeObject<ResponseContract<BoardEvalPayload>>(json);

            Assert.NotNull(boardEval);
            Assert.Equal("OK", boardEval.Type);
            Assert.IsType<BoardEvalPayload>(boardEval.Payload);
            Assert.InRange(boardEval.Payload.EvalScore, 0.4, 0.5);
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
                Board = boardContract,
                Modus = modus,
                IsWhite = isWhite
            };

            var response = await client.PostAsJsonAsync("/bot/mars/api/eval/board", boardStateRequest);
            var json = await response.Content.ReadAsStringAsync();
            var boardEval = JsonConvert.DeserializeObject<ResponseContract<BoardEvalPayload>>(json);

            Assert.NotNull(boardEval);
            Assert.Equal("OK", boardEval.Type);
            Assert.IsType<BoardEvalPayload>(boardEval.Payload);
            if (!isWhite)
            {
                // black has stronger board
                Assert.InRange(boardEval.Payload.EvalScore, 1.2, 1.7);
            }
            else
            {
                Assert.InRange(boardEval.Payload.EvalScore, -1.5, -1.0);
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
                Board = boardContract,
                Modus = modus,
                IsWhite = isWhite
            };

            var response = await client.PostAsJsonAsync("/bot/mars/api/eval/board", boardStateRequest);
            var json = await response.Content.ReadAsStringAsync();
            var boardEval = JsonConvert.DeserializeObject<ResponseContract<BoardEvalPayload>>(json);

            Assert.NotNull(boardEval);
            Assert.Equal("OK", boardEval.Type);
            Assert.IsType<BoardEvalPayload>(boardEval.Payload);
            if (!isWhite)
            {
                // black has stronger board
                Assert.InRange(boardEval.Payload.EvalScore, 0.5, 0.7);
            }
            else
            {
                Assert.InRange(boardEval.Payload.EvalScore, 0.2, 0.4);
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
                Board = boardContract,
                Modus = modus,
                IsWhite = isWhite,
                Rolls = new[] { 2, 1 },
                BotLevel = BotLevel.Hard
            };

            var response = await client.PostAsJsonAsync("/bot/mars/api/eval/move", moveRequest);
            var json = await response.Content.ReadAsStringAsync();
            var moveEval = JsonConvert.DeserializeObject<ResponseContract<MoveEvalPayload>>(json);

            Assert.NotNull(moveEval);
            Assert.Equal("OK", moveEval.Type);
            Assert.IsType<MoveEvalPayload>(moveEval.Payload);
            Assert.NotNull(moveEval.Payload.MoveSequence);
            Assert.Equal(2, moveEval.Payload.MoveSequence.Moves.Count);
            if (!isWhite)
            {
                Assert.Equal(23, moveEval.Payload.MoveSequence.Moves[0].From);
                Assert.Equal(22, moveEval.Payload.MoveSequence.Moves[0].To);
                Assert.Equal(22, moveEval.Payload.MoveSequence.Moves[1].From);
                Assert.Equal(20, moveEval.Payload.MoveSequence.Moves[1].To);
            }
            else
            {
                Assert.Equal(0, moveEval.Payload.MoveSequence.Moves[0].From);
                Assert.Equal(2, moveEval.Payload.MoveSequence.Moves[0].To);
                Assert.Equal(2, moveEval.Payload.MoveSequence.Moves[1].From);
                Assert.Equal(3, moveEval.Payload.MoveSequence.Moves[1].To);
            }
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
                Board = boardContract,
                Modus = modus,
                IsWhite = isWhite,
                Rolls = new[] { 2, 1 },
                BotLevel = BotLevel.Hard
            };

            var response = await client.PostAsJsonAsync("/bot/mars/api/eval/move", moveRequest);
            var json = await response.Content.ReadAsStringAsync();
            var moveEval = JsonConvert.DeserializeObject<ResponseContract<MoveEvalPayload>>(json);

            Assert.NotNull(moveEval);
            Assert.Equal("OK", moveEval.Type);
            Assert.IsType<MoveEvalPayload>(moveEval.Payload);
            Assert.NotNull(moveEval.Payload.MoveSequence);
            Assert.Equal(2, moveEval.Payload.MoveSequence.Moves.Count);
            if (!isWhite)
            {
                Assert.Equal(18, moveEval.Payload.MoveSequence.Moves[0].From);
                Assert.Equal(16, moveEval.Payload.MoveSequence.Moves[0].To);
                Assert.Equal(16, moveEval.Payload.MoveSequence.Moves[1].From);
                Assert.Equal(15, moveEval.Payload.MoveSequence.Moves[1].To);
            }
            else
            {
                Assert.Equal(-1, moveEval.Payload.MoveSequence.Moves[0].From);
                Assert.Equal(0, moveEval.Payload.MoveSequence.Moves[0].To);
                Assert.Equal(-1, moveEval.Payload.MoveSequence.Moves[1].From);
                Assert.Equal(1, moveEval.Payload.MoveSequence.Moves[1].To);
            }
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
                Board = boardContract,
                Modus = modus,
                IsWhite = isWhite,
                Rolls = new[] { 2, 1 },
                BotLevel = BotLevel.Hard
            };

            var response = await client.PostAsJsonAsync("/bot/mars/api/eval/move", moveRequest);
            var json = await response.Content.ReadAsStringAsync();
            var moveEval = JsonConvert.DeserializeObject<ResponseContract<MoveEvalPayload>>(json);

            Assert.NotNull(moveEval);
            Assert.Equal("OK", moveEval.Type);
            Assert.IsType<MoveEvalPayload>(moveEval.Payload);
            Assert.Equal(2, moveEval.Payload.MoveSequence.Moves.Count);
            if (!isWhite)
            {
                Assert.Equal(15, moveEval.Payload.MoveSequence.Moves[0].From);
                Assert.Equal(16, moveEval.Payload.MoveSequence.Moves[0].To);
                Assert.Equal(17, moveEval.Payload.MoveSequence.Moves[1].From);
                Assert.Equal(19, moveEval.Payload.MoveSequence.Moves[1].To);
            }
            else
            {
                Assert.Equal(0, moveEval.Payload.MoveSequence.Moves[0].From);
                Assert.Equal(2, moveEval.Payload.MoveSequence.Moves[0].To);
                Assert.Equal(-1, moveEval.Payload.MoveSequence.Moves[1].From);
                Assert.Equal(0, moveEval.Payload.MoveSequence.Moves[1].To);
            }
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
                Board = boardContract,
                Modus = modus,
                IsWhite = isWhite,
                MatchLength = 1,
                PointsAwayOpp = 1,
                PointsAwayPlayer = 1,
                BotLevel = BotLevel.Hard
            };

            var response = await client.PostAsJsonAsync("/bot/mars/api/eval/cube", cubeRequest);
            var json = await response.Content.ReadAsStringAsync();
            var cubeEval = JsonConvert.DeserializeObject<ResponseContract<CubeEvalPayload>>(json);

            Assert.NotNull(cubeEval);
            Assert.Equal("OK", cubeEval.Type);
            Assert.IsType<CubeEvalPayload>(cubeEval.Payload);
            if (!isWhite)
            {
                Assert.Equal(CubeAction.NoDouble, cubeEval.Payload.CubeAction);
            }
            else
            {
                Assert.Equal(CubeAction.NoDouble, cubeEval.Payload.CubeAction);
            }
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
                Board = boardContract,
                Modus = modus,
                IsWhite = isWhite,
                MatchLength = 1,
                PointsAwayOpp = 1,
                PointsAwayPlayer = 1,
                BotLevel = BotLevel.Hard
            };

            var response = await client.PostAsJsonAsync("/bot/mars/api/eval/cube", cubeRequest);
            var json = await response.Content.ReadAsStringAsync();
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
