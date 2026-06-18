using GammonX.Engine.Extensions;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;
using GammonX.Mars.NN.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Moq;

using Newtonsoft.Json;

namespace GammonX.Mars.NN.Tests.Services
{
    public class DefaultFeatureEvalServiceTests
    {
        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(1, 3)]
        [InlineData(1, 4)]
        [InlineData(1, 5)]
        [InlineData(1, 6)]
        [InlineData(2, 2)]
        [InlineData(2, 3)]
        [InlineData(2, 4)]
        [InlineData(2, 5)]
        [InlineData(2, 6)]
        [InlineData(3, 3)]
        [InlineData(3, 4)]
        [InlineData(3, 5)]
        [InlineData(3, 6)]
        [InlineData(4, 4)]
        [InlineData(4, 5)]
        [InlineData(4, 6)]
        [InlineData(5, 5)]
        [InlineData(5, 6)]
        [InlineData(6, 6)]
        public void CanEvalBackgammonStartBoardForWhiteAndBlack(int roll1, int roll2)
        {
            var modus = GameModus.Backgammon;
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var boardContract = board.ToContract(false);
            Assert.NotNull(boardContract);

            var raceWeights = EvalWeights.GetRaceWeights(modus);
            var contactWeights = EvalWeights.GetContactWeights(modus);
            var cheapContactWeights = EvalWeights.GetCheapContactWeights(modus);

            contactWeights.Validate();
            raceWeights.Validate();
            cheapContactWeights.Validate();

            var evalService = new DefaultFeatureEvalService(neuralService: null!, modus);
            var requestWhite = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = true,
                BotLevel = BotLevel.Hard
            };
            var resultWhite = evalService.EvalMoveSequence(requestWhite, cheapContactWeights, contactWeights, raceWeights, 20);
            Assert.NotNull(resultWhite);

            var requestBlack = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = false,
                BotLevel = BotLevel.Hard
            };
            var resultBlack = evalService.EvalMoveSequence(requestBlack, cheapContactWeights, contactWeights, raceWeights, 20);
            Assert.NotNull(resultBlack);

            var invertedBlack = resultBlack.Moves.Select(m => m.Invert(modus));
            invertedBlack = invertedBlack.OrderBy(m => m.From).ThenBy(m => m.To);
            var sortedWhite = resultWhite.Moves.OrderBy(m => m.From).ThenBy(m => m.To);
            Assert.Equal(sortedWhite, invertedBlack);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(1, 3)]
        [InlineData(1, 4)]
        [InlineData(1, 5)]
        [InlineData(1, 6)]
        [InlineData(2, 2)]
        [InlineData(2, 3)]
        [InlineData(2, 4)]
        [InlineData(2, 5)]
        [InlineData(2, 6)]
        [InlineData(3, 3)]
        [InlineData(3, 4)]
        [InlineData(3, 5)]
        [InlineData(3, 6)]
        [InlineData(4, 4)]
        [InlineData(4, 5)]
        [InlineData(4, 6)]
        [InlineData(5, 5)]
        [InlineData(5, 6)]
        [InlineData(6, 6)]
        public void CanEvalTavlaStartBoardForWhiteAndBlack(int roll1, int roll2)
        {
            var modus = GameModus.Tavla;
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var boardContract = board.ToContract(false);
            Assert.NotNull(boardContract);

            var raceWeights = EvalWeights.GetRaceWeights(modus);
            var contactWeights = EvalWeights.GetContactWeights(modus);
            var cheapContactWeights = EvalWeights.GetCheapContactWeights(modus);

            contactWeights.Validate();
            raceWeights.Validate();
            cheapContactWeights.Validate();

            var evalService = new DefaultFeatureEvalService(neuralService: null!, modus);
            var requestWhite = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = true,
                BotLevel = BotLevel.Hard
            };
            var resultWhite = evalService.EvalMoveSequence(requestWhite, cheapContactWeights, contactWeights, raceWeights, 20);
            Assert.NotNull(resultWhite);

            var requestBlack = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = false,
                BotLevel = BotLevel.Hard
            };
            var resultBlack = evalService.EvalMoveSequence(requestBlack, cheapContactWeights, contactWeights, raceWeights, 20);
            Assert.NotNull(resultBlack);

            var invertedBlack = resultBlack.Moves.Select(m => m.Invert(modus));
            invertedBlack = invertedBlack.OrderBy(m => m.From).ThenBy(m => m.To);
            var sortedWhite = resultWhite.Moves.OrderBy(m => m.From).ThenBy(m => m.To);
            Assert.Equal(sortedWhite, invertedBlack);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(1, 3)]
        [InlineData(1, 4)]
        [InlineData(1, 5)]
        [InlineData(1, 6)]
        [InlineData(2, 2)]
        [InlineData(2, 3)]
        [InlineData(2, 4)]
        [InlineData(2, 5)]
        [InlineData(2, 6)]
        [InlineData(3, 3)]
        [InlineData(3, 4)]
        [InlineData(3, 5)]
        [InlineData(3, 6)]
        [InlineData(4, 4)]
        [InlineData(4, 5)]
        [InlineData(4, 6)]
        [InlineData(5, 5)]
        [InlineData(5, 6)]
        [InlineData(6, 6)]
        public void CanEvalPortesStartBoardForWhiteAndBlack(int roll1, int roll2)
        {
            var modus = GameModus.Portes;
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var boardContract = board.ToContract(false);
            Assert.NotNull(boardContract);

            var raceWeights = EvalWeights.GetRaceWeights(modus);
            var contactWeights = EvalWeights.GetContactWeights(modus);
            var cheapContactWeights = EvalWeights.GetCheapContactWeights(modus);

            contactWeights.Validate();
            raceWeights.Validate();
            cheapContactWeights.Validate();

            var evalService = new DefaultFeatureEvalService(neuralService: null!, modus);
            var requestWhite = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = true,
                BotLevel = BotLevel.Hard
            };
            var resultWhite = evalService.EvalMoveSequence(requestWhite, cheapContactWeights, contactWeights, raceWeights, 20);
            Assert.NotNull(resultWhite);

            var requestBlack = new EvalMoveRequestContract()
            {
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = false,
                BotLevel = BotLevel.Hard
            };
            var resultBlack = evalService.EvalMoveSequence(requestBlack, cheapContactWeights, contactWeights, raceWeights, 20);
            Assert.NotNull(resultBlack);

            var invertedBlack = resultBlack.Moves.Select(m => m.Invert(modus));
            invertedBlack = invertedBlack.OrderBy(m => m.From).ThenBy(m => m.To);
            var sortedWhite = resultWhite.Moves.OrderBy(m => m.From).ThenBy(m => m.To);
            Assert.Equal(sortedWhite, invertedBlack);
        }

        [Theory]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        public void CannotEvalCube(GameModus modus)
        {
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.DefaultBoard1);
            Assert.NotNull(boardContract);

            var evalService = new DefaultFeatureEvalService(default(INeuralEvalService)!, modus);

            EvalCubeRequestContract request = new EvalCubeRequestContract()
            {
                Board = boardContract,
                Modus = modus,
                IsWhite = false,
                MatchLength = 2,
                PointsAwayOpp = 1,
                PointsAwayPlayer = 1,
                BotLevel = BotLevel.Hard
            };

            Assert.Throws<InvalidOperationException>(() => evalService.EvalCube(request));
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        public void BotMustAcceptDouble(GameModus modus)
        {
            var modelPath = Path.Combine("Data/NeuralNets", $"{modus}", "training_net.dat");
            var nnEvalService = NeuralEvalService.Load(modus, modelPath);
            var evalService = FeatureEvalServiceFactory.Create(modus, nnEvalService);

            var evalCubeReq = JsonConvert.DeserializeObject<EvalCubeRequestContract>(MockRequests.CubeEvalRequestMustOfferDouble);
            Assert.NotNull(evalCubeReq);

            var (shouldOffer, shouldTake) = evalService.EvalCube(evalCubeReq);
            Assert.Equal(CubeAction.NoDouble, shouldOffer);
            Assert.Equal(CubeAction.Take, shouldTake);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        public void BotOffersInstantDoubleIfBehindBig(GameModus modus)
        {
            var modelPath = Path.Combine("Data/NeuralNets", $"{modus}", "training_net.dat");
            var nnEvalService = NeuralEvalService.Load(modus, modelPath);
            var evalService = FeatureEvalServiceFactory.Create(modus, nnEvalService);

            var evalCubeReq = JsonConvert.DeserializeObject<EvalCubeRequestContract>(MockRequests.CubeEvalRequestOnePlayerIsBehindBig);
            Assert.NotNull(evalCubeReq);

            var (shouldOffer, shouldTake) = evalService.EvalCube(evalCubeReq);
            Assert.Equal(CubeAction.Double, shouldOffer);
            Assert.Equal(CubeAction.Take, shouldTake);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        public void BotMustTakeInstantDoubleIfOppBehindBig(GameModus modus)
        {
            var modelPath = Path.Combine("Data/NeuralNets", $"{modus}", "training_net.dat");
            var nnEvalService = NeuralEvalService.Load(modus, modelPath);
            var evalService = FeatureEvalServiceFactory.Create(modus, nnEvalService);

            var evalCubeReq = JsonConvert.DeserializeObject<EvalCubeRequestContract>(MockRequests.CubeEvalRequestBotMustTakeInstantDoubleIfOppBehindBig);
            Assert.NotNull(evalCubeReq);

            var (shouldOffer, shouldTake) = evalService.EvalCube(evalCubeReq);
            Assert.Equal(CubeAction.NoDouble, shouldOffer);
            Assert.Equal(CubeAction.Take, shouldTake);
        }

        [Fact]
        public void EvalCubeReturnsNoDoubleWhenTakeIsJustSlightlyBetter()
        {
            var neural = new Mock<INeuralEvalService>();

            neural.Setup(x => x.Predict(
                    It.IsAny<NormalizedEvalResultModel>(),
                    It.IsAny<IBoardModel>(),
                    It.IsAny<bool>()))
                .Returns(new []
                {
                    0.50f, // win
                    0.01f, // gammon win
                    0.00f, // bg win
                    0.01f, // gammon loss
                    0.00f  // bg loss
                });

            var service = new DefaultFeatureEvalService(neural.Object, GameModus.Backgammon);

            var (shouldOffer, shouldTake) = service.EvalCube(CreateRequest());

            Assert.Equal(CubeAction.NoDouble, shouldOffer);
            Assert.Equal(CubeAction.Take, shouldTake);
        }

        [Fact]
        public void EvalCubeReturnsDoubleWhenOppPassesAndPassEquityBetterThanNoDouble()
        {
            var neural = new Mock<INeuralEvalService>();

            neural.Setup(x => x.Predict(
                    It.IsAny<NormalizedEvalResultModel>(),
                    It.IsAny<IBoardModel>(),
                    It.IsAny<bool>()))
                .Returns(new []
                {
                    0.75f, // win
                    0.20f,
                    0.05f,
                    0.05f,
                    0.00f
                });

            var service = new DefaultFeatureEvalService(neural.Object, GameModus.Backgammon);

            var (shouldOffer, shouldTake) = service.EvalCube(CreateRequest());

            // At 4-away 4-away: equityIfOppPasses (0.59) > noDouble (0.566)
            // Opponent would pass, and the pass equity exceeds no-double equity → Double
            Assert.Equal(CubeAction.Double, shouldOffer);
            Assert.Equal(CubeAction.Take, shouldTake);
        }

        [Fact]
        public void EvalCubeReturnsTooGoodWhenPlayingOnForGammonBeatsForcingPass()
        {
            var neural = new Mock<INeuralEvalService>();

            neural.Setup(x => x.Predict(
                    It.IsAny<NormalizedEvalResultModel>(),
                    It.IsAny<IBoardModel>(),
                    It.IsAny<bool>()))
                .Returns(new []
                {
                    0.99f, // win
                    0.90f, // gammon win
                    0.50f, // bg win
                    0.00f,
                    0.00f
                });

            var service = new DefaultFeatureEvalService(neural.Object, GameModus.Backgammon);

            var (shouldOffer, shouldTake) = service.EvalCube(CreateRequest());

            // At 4-away 4-away: noDouble (0.744) > equityIfOppPasses (0.59)
            // Opponent would pass, but playing on for gammon/backgammon is even better → TooGood
            Assert.Equal(CubeAction.TooGood, shouldOffer);
            Assert.Equal(CubeAction.Take, shouldTake);
        }

        [Fact]
        public void EvalCubeReturnsNoDoubleAtDoubleMatchPoint()
        {
            var neural = new Mock<INeuralEvalService>();

            neural.Setup(x => x.Predict(
                    It.IsAny<NormalizedEvalResultModel>(),
                    It.IsAny<IBoardModel>(),
                    It.IsAny<bool>()))
                .Returns(new []
                {
                    0.99f, // win
                    0.90f,
                    0.50f,
                    0.00f,
                    0.00f
                });

            var service = new DefaultFeatureEvalService(
                neural.Object,
                GameModus.Backgammon);

            var request = new EvalCubeRequestContract
            {
                Modus = GameModus.Backgammon,
                IsWhite = true,
                MatchLength = 3,
                PointsAwayPlayer = 1,
                PointsAwayOpp = 1,
                Board = new BoardModelContract
                {
                    Fields = new int[24],
                    DoublingCubeValue = 1
                },
                BotLevel = BotLevel.Hard
            };

            var (shouldOffer, shouldTake) = service.EvalCube(request);

            // At double match point (1-away, 1-away): doubleTake = noDouble = WinP
            // Doubling has no equity benefit since any win wins the match regardless of cube
            Assert.Equal(CubeAction.NoDouble, shouldOffer);
            Assert.Equal(CubeAction.Take, shouldTake);
        }

        private static EvalCubeRequestContract CreateRequest()
        {
            return new EvalCubeRequestContract
            {
                Modus = GameModus.Backgammon,
                IsWhite = true,
                MatchLength = 7,
                PointsAwayPlayer = 4,
                PointsAwayOpp = 4,
                Board = new BoardModelContract
                {
                    Fields = new int[24],
                    DoublingCubeValue = 1
                }
            };
        }
    }
}
