using GammonX.Engine.Services;
using GammonX.Mars.Server.Features;

using GammonX.Mars.Server.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Mars.Server.Tests.Features
{
    public class RaceFeatureTests
    {
        [Fact]
        public void CanEvalPlakotoBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PlakotoBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new RaceFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.False(whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.False(blackResult);
        }


        [Fact]
        public void CanEvalPlakotoRaceBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PlakotoRaceBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new RaceFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.True(whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.True(blackResult);
        }

        [Fact]
        public void CanEvalPlakotoRacePinBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PlakotoRacePinBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new RaceFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.False(whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.False(blackResult);
        }

        [Fact]
        public void CanEvalPlakotoBorneOffBoard1()
        {
            var boardService = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PlakotoBorneOffBoard1);
            Assert.NotNull(boardContract);
            var board = boardService.CreateBoard(boardContract);

            var feature = new RaceFeature();
            var whiteResult = feature.Eval(board, true);
            Assert.True(whiteResult);
            var blackResult = feature.Eval(board, false);
            Assert.True(blackResult);
        }
    }
}
