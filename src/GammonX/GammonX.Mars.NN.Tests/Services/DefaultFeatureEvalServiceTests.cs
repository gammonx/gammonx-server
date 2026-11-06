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

using static TorchSharp.torch;

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
        public async Task CanEvalTwoPlyBackgammonStartBoard(int roll1, int roll2)
        {
            var modus = GameModus.Backgammon;
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var boardContract = board.ToContract(false);
            Assert.NotNull(boardContract);

            var modelPath = Path.Combine("Data/NeuralNets", $"{modus}", "training_net.dat");
            var device = cuda.is_available() ? CUDA : CPU;
            var nnEvalService = BatchedNeuralEvalService.Load(modus, modelPath, device);
            var cancellationTokenSource = new CancellationTokenSource();
            await ((BatchedNeuralEvalService)nnEvalService).StartAsync(cancellationTokenSource.Token);
            var evalService = FeatureEvalServiceFactory.Create(modus, nnEvalService);
            Assert.NotNull(evalService);

            var contactWeights = EvalWeights.GetContactWeights(modus);
            contactWeights.Validate();

            var requestWhite = new EvalMoveRequestContract
            {
                TurnNumber = 0,
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = true,
                BotLevel = BotLevel.Expert
            };
            var resultWhite = await evalService.EvalMoveSequencesAsync(new FeatureEvalOptions
            {
                EvalMoveRequest = requestWhite,
                ContactWeights = contactWeights,
                TurnNumber = requestWhite.TurnNumber,
            });
            Assert.NotNull(resultWhite);

            await cancellationTokenSource.CancelAsync();
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
        public async Task CanEvalBackgammonStartBoardForWhiteAndBlack(int roll1, int roll2)
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
                TurnNumber = 0,
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = true,
                BotLevel = BotLevel.Hard
            };
            var resultWhite = await evalService.EvalMoveSequencesAsync(new FeatureEvalOptions
            {
                EvalMoveRequest = requestWhite,
                ContactWeights = contactWeights,
                MaxCandidates = 20,
                TurnNumber = requestWhite.TurnNumber,
            });
            Assert.NotNull(resultWhite);

            var requestBlack = new EvalMoveRequestContract()
            {
                TurnNumber = 0,
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = false,
                BotLevel = BotLevel.Hard
            };
            var resultBlack = await evalService.EvalMoveSequencesAsync(new FeatureEvalOptions
            {
                EvalMoveRequest = requestBlack,
                ContactWeights = contactWeights,
                MaxCandidates = 20,
                TurnNumber = requestBlack.TurnNumber,
            });
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
        public async Task CanEvalTavlaStartBoardForWhiteAndBlack(int roll1, int roll2)
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
                TurnNumber = 0,
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = true,
                BotLevel = BotLevel.Hard
            };
            var resultWhite = await evalService.EvalMoveSequencesAsync(new FeatureEvalOptions
            {
                EvalMoveRequest = requestWhite,
                ContactWeights = contactWeights,
                MaxCandidates = 20,
                TurnNumber = requestWhite.TurnNumber,
            });
            Assert.NotNull(resultWhite);

            var requestBlack = new EvalMoveRequestContract()
            {
                TurnNumber = 0,
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = false,
                BotLevel = BotLevel.Hard
            };
            var resultBlack = await evalService.EvalMoveSequencesAsync(new FeatureEvalOptions
            {
                EvalMoveRequest = requestBlack,
                ContactWeights = contactWeights,
                MaxCandidates = 20,
                TurnNumber = requestBlack.TurnNumber,
            });
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
        public async Task CanEvalPortesStartBoardForWhiteAndBlack(int roll1, int roll2)
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
                TurnNumber = 0,
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = true,
                BotLevel = BotLevel.Hard
            };
            var resultWhite = await evalService.EvalMoveSequencesAsync(new FeatureEvalOptions
            {
                EvalMoveRequest = requestWhite,
                ContactWeights = contactWeights,
                MaxCandidates = 20,
                TurnNumber = requestWhite.TurnNumber,
            });
            Assert.NotNull(resultWhite);

            var requestBlack = new EvalMoveRequestContract()
            {
                TurnNumber = 0,
                Board = boardContract,
                Modus = modus,
                Rolls = [roll1, roll2],
                IsWhite = false,
                BotLevel = BotLevel.Hard
            };
            var resultBlack = await evalService.EvalMoveSequencesAsync(new FeatureEvalOptions
            {
                EvalMoveRequest = requestBlack,
                ContactWeights = contactWeights,
                MaxCandidates = 20,
                TurnNumber = requestBlack.TurnNumber,
            });
            Assert.NotNull(resultBlack);

            var invertedBlack = resultBlack.Moves.Select(m => m.Invert(modus));
            invertedBlack = invertedBlack.OrderBy(m => m.From).ThenBy(m => m.To);
            var sortedWhite = resultWhite.Moves.OrderBy(m => m.From).ThenBy(m => m.To);
            Assert.Equal(sortedWhite, invertedBlack);
        }

        [Theory]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        public async Task CannotEvalCube(GameModus modus)
        {
            var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.DefaultBoard1);
            Assert.NotNull(boardContract);

            var evalService = new DefaultFeatureEvalService(default(INeuralEvalService)!, modus);

            EvalCubeRequestContract request = new EvalCubeRequestContract()
            {
                TurnNumber = 0,
                Board = boardContract,
                Modus = modus,
                IsWhite = false,
                MatchLength = 2,
                PointsAwayOpp = 1,
                PointsAwayPlayer = 1,
                BotLevel = BotLevel.Hard
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await evalService.EvalCubeAsync(new FeatureEvalOptions
            {
                EvalCubeRequest = request,
                IsWhite = request.IsWhite,
                TurnNumber = request.TurnNumber,
            }));
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        public async Task BotMustAcceptDouble(GameModus modus)
        {
            var modelPath = Path.Combine("Data/NeuralNets", $"{modus}", "training_net.dat");
            var device = cuda.is_available() ? CUDA : CPU;
            var nnEvalService = NeuralEvalService.Load(modus, modelPath, device);
            var evalService = FeatureEvalServiceFactory.Create(modus, nnEvalService);

            var evalCubeReq = JsonConvert.DeserializeObject<EvalCubeRequestContract>(MockRequests.CubeEvalRequestMustOfferDouble);
            Assert.NotNull(evalCubeReq);

            var (shouldOffer, shouldTake) = await evalService.EvalCubeAsync(new FeatureEvalOptions
            {
                EvalCubeRequest = evalCubeReq,
                IsWhite = evalCubeReq.IsWhite,
                TurnNumber = evalCubeReq.TurnNumber,
            });
            Assert.Equal(CubeAction.NoDouble, shouldOffer);
            Assert.Equal(CubeAction.Take, shouldTake);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        public async Task BotOffersInstantDoubleIfBehindBig(GameModus modus)
        {
            var modelPath = Path.Combine("Data/NeuralNets", $"{modus}", "training_net.dat");
            var nnEvalService = NeuralEvalService.Load(modus, modelPath, CPU);
            var evalService = FeatureEvalServiceFactory.Create(modus, nnEvalService);

            var evalCubeReq = JsonConvert.DeserializeObject<EvalCubeRequestContract>(MockRequests.CubeEvalRequestOnePlayerIsBehindBig);
            Assert.NotNull(evalCubeReq);

            var (shouldOffer, shouldTake) = await evalService.EvalCubeAsync(new FeatureEvalOptions
            {
                EvalCubeRequest = evalCubeReq,
                IsWhite = evalCubeReq.IsWhite,
                TurnNumber = evalCubeReq.TurnNumber,
            });
            Assert.Equal(CubeAction.Double, shouldOffer);
            Assert.Equal(CubeAction.Take, shouldTake);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        public async Task BotAcceptsEarlyDouble(GameModus modus)
        {
            var modelPath = Path.Combine("Data/NeuralNets", $"{modus}", "training_net.dat");
            var nnEvalService = NeuralEvalService.Load(modus, modelPath, CPU);
            var evalService = FeatureEvalServiceFactory.Create(modus, nnEvalService);

            var evalCubeReq = JsonConvert.DeserializeObject<EvalCubeRequestContract>(MockRequests.CubeEvaleRequestBotOffersEarlyDouble);
            Assert.NotNull(evalCubeReq);

            var (shouldOffer, shouldTake) = await evalService.EvalCubeAsync(new FeatureEvalOptions
            {
                EvalCubeRequest = evalCubeReq,
                IsWhite = evalCubeReq.IsWhite,
                TurnNumber = evalCubeReq.TurnNumber,
            });
            // we expect the bot to not double this early on
            Assert.Equal(CubeAction.NoDouble, shouldOffer);
            // but we expect the bot to take the double in early stages
            Assert.Equal(CubeAction.Take, shouldTake);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        public async Task BotMustTakeInstantDoubleIfOppBehindBig(GameModus modus)
        {
            var modelPath = Path.Combine("Data/NeuralNets", $"{modus}", "training_net.dat");
            var nnEvalService = NeuralEvalService.Load(modus, modelPath, CPU);
            var evalService = FeatureEvalServiceFactory.Create(modus, nnEvalService);

            var evalCubeReq = JsonConvert.DeserializeObject<EvalCubeRequestContract>(MockRequests.CubeEvalRequestBotMustTakeInstantDoubleIfOppBehindBig);
            Assert.NotNull(evalCubeReq);

            var (shouldOffer, shouldTake) = await evalService.EvalCubeAsync(new FeatureEvalOptions
            {
                EvalCubeRequest = evalCubeReq,
                IsWhite = evalCubeReq.IsWhite,
                TurnNumber = evalCubeReq.TurnNumber,
            });
            Assert.Equal(CubeAction.NoDouble, shouldOffer);
            Assert.Equal(CubeAction.Take, shouldTake);
        }

        [Fact]
        public async Task EvalCubeReturnsNoDoubleWhenTakeIsJustSlightlyBetter()
        {
            var neural = new Mock<INeuralEvalService>();

            neural.Setup(x => x.PredictAsync(
                    It.IsAny<NormalizedEvalResultModel>(),
                    It.IsAny<IBoardModel>(),
                    It.IsAny<bool>()))
                .Returns(Task.FromResult(new[]
                {
                    0.10f, // win
                    0.00f, // gammon win
                    0.00f, // bg win
                    0.00f, // gammon loss
                    0.00f  // bg loss
                }));

            var service = new DefaultFeatureEvalService(neural.Object, GameModus.Backgammon);

            var (shouldOffer, shouldTake) = await service.EvalCubeAsync(
                CreateRequest(isWhite: true, pointsAwayPlayer: 1, pointsAwayOpp: 4, cubeValue: 1));

            Assert.Equal(CubeAction.Pass, shouldTake);
            Assert.Equal(CubeAction.NoDouble, shouldOffer);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EvalCubeDoesNotForceTakeWhenPassingHasHigherEquity(bool isWhite)
        {
            var neural = new Mock<INeuralEvalService>();

            neural.Setup(x => x.PredictAsync(
                    It.IsAny<NormalizedEvalResultModel>(),
                    It.IsAny<IBoardModel>(),
                    It.IsAny<bool>()))
                .Returns(Task.FromResult(new[]
                {
                    0.10f, // win
                    0.00f, // gammon win
                    0.00f, // bg win
                    0.00f, // gammon loss
                    0.00f  // bg loss
                }));

            var service = new DefaultFeatureEvalService(neural.Object, GameModus.Backgammon);

            var (shouldOffer, shouldTake) = await service.EvalCubeAsync(CreateRequest(
                pointsAwayPlayer: 1,
                pointsAwayOpp: 4,
                isWhite: isWhite,
                cubeValue: 1));

            Assert.Equal(CubeAction.Pass, shouldTake);
            Assert.Equal(CubeAction.NoDouble, shouldOffer);
        }

        [Theory]
        [InlineData(4, 2, 1)]
        [InlineData(6, 4, 2)]
        public async Task EvalCubeUsesAutomaticRecubeWhenPlayerTakesDouble(int pointsAwayPlayer, int pointsAwayOpp, int cubeValue)
        {
            var neural = new Mock<INeuralEvalService>();

            neural.Setup(x => x.PredictAsync(
                    It.IsAny<NormalizedEvalResultModel>(),
                    It.IsAny<IBoardModel>(),
                    It.IsAny<bool>()))
                .Returns(Task.FromResult(new[]
                {
                    0.40f, // win
                    0.00f, // gammon win
                    0.00f, // bg win
                    0.00f, // gammon loss
                    0.00f  // bg loss
                }));

            var service = new DefaultFeatureEvalService(neural.Object, GameModus.Backgammon);

            var (shouldOffer, shouldTake) = await service.EvalCubeAsync(CreateRequest(
                isWhite: true,
                pointsAwayPlayer: pointsAwayPlayer,
                pointsAwayOpp: pointsAwayOpp,
                cubeValue: cubeValue));

            Assert.Equal(CubeAction.Take, shouldTake);
            Assert.Equal(CubeAction.NoDouble, shouldOffer);
        }

        [Fact]
        public async Task EvalCubeReturnsDoubleWhenOppPassesAndPassEquityBetterThanNoDouble()
        {
            var neural = new Mock<INeuralEvalService>();

            neural.Setup(x => x.PredictAsync(
                    It.IsAny<NormalizedEvalResultModel>(),
                    It.IsAny<IBoardModel>(),
                    It.IsAny<bool>()))
                .Returns(Task.FromResult(new[]
                {
                    0.75f, // win
                    0.20f,
                    0.05f,
                    0.05f,
                    0.00f
                }));

            var service = new DefaultFeatureEvalService(neural.Object, GameModus.Backgammon);

            var (shouldOffer, shouldTake) = await service.EvalCubeAsync(
                CreateRequest(isWhite: true, pointsAwayPlayer: 1, pointsAwayOpp: 4, cubeValue: 1));

            // Opponent would pass, and the pass equity exceeds no-double equity.
            Assert.Equal(CubeAction.NoDouble, shouldOffer);
            Assert.Equal(CubeAction.Take, shouldTake);
        }

        [Fact]
        public async Task EvalCubeReturnsTooGoodWhenPlayingOnForGammonBeatsForcingPass()
        {
            var neural = new Mock<INeuralEvalService>();

            neural.Setup(x => x.PredictAsync(
                    It.IsAny<NormalizedEvalResultModel>(),
                    It.IsAny<IBoardModel>(),
                    It.IsAny<bool>()))
                .Returns(Task.FromResult(new[]
                {
                    0.99f, // win
                    0.90f, // gammon win
                    0.50f, // bg win
                    0.00f,
                    0.00f
                }));

            var service = new DefaultFeatureEvalService(neural.Object, GameModus.Backgammon);

            var (shouldOffer, shouldTake) = await service.EvalCubeAsync(
                CreateRequest(isWhite: true, pointsAwayPlayer: 1, pointsAwayOpp: 4, cubeValue: 1));

            // Opponent would pass, but playing on for gammon/backgammon is even better.
            Assert.Equal(CubeAction.NoDouble, shouldOffer);
            Assert.Equal(CubeAction.Take, shouldTake);
        }

        [Fact]
        public async Task EvalCubeReturnsNoDoubleAtDoubleMatchPoint()
        {
            var neural = new Mock<INeuralEvalService>();

            neural.Setup(x => x.PredictAsync(
                    It.IsAny<NormalizedEvalResultModel>(),
                    It.IsAny<IBoardModel>(),
                    It.IsAny<bool>()))
                .Returns(Task.FromResult(new[]
                {
                    0.99f, // win
                    0.90f,
                    0.50f,
                    0.00f,
                    0.00f
                }));

            var service = new DefaultFeatureEvalService(
                neural.Object,
                GameModus.Backgammon);

            var request = new EvalCubeRequestContract
            {
                TurnNumber = 0,
                Modus = GameModus.Backgammon,
                IsWhite = true,
                MatchLength = 3,
                PointsAwayPlayer = 1,
                PointsAwayOpp = 1,
                Board = new BoardModelContract
                {
                    Fields = BoardServiceFactory.Create(GameModus.Backgammon).CreateBoard().Fields,
                    DoublingCubeValue = 1
                },
                BotLevel = BotLevel.Hard
            };

            var (shouldOffer, shouldTake) = await service.EvalCubeAsync(new FeatureEvalOptions
            {
                EvalCubeRequest = request,
                IsWhite = request.IsWhite,
                TurnNumber = request.TurnNumber,
            });

            // At double match point (1-away, 1-away): doubleTake = noDouble = WinP
            // Doubling has no equity benefit since any win wins the match regardless of cube
            Assert.Equal(CubeAction.NoDouble, shouldOffer);
            Assert.Equal(CubeAction.Take, shouldTake);
        }

        [Theory]
        [InlineData(true, 15, 1, 0, 0, 1)]
        [InlineData(true, 15, 0, 0, 0, 2)]
        [InlineData(true, 15, 0, 0, 1, 3)]
        [InlineData(false, 15, 1, 0, 0, -1)]
        [InlineData(false, 15, 0, 0, 0, -2)]
        [InlineData(false, 15, 0, 0, 1, -3)]
        public async Task EvalBoardStateUsesTerminalGameEquityWithoutNeural(
            bool isWhite,
            int bearOffCountWhite,
            int bearOffCountBlack,
            int homeBarCountWhite,
            int homeBarCountBlack,
            double expectedScore)
        {
            var neural = new Mock<INeuralEvalService>();
            var service = new DefaultFeatureEvalService(neural.Object, GameModus.Backgammon);
            var request = new EvalBoardRequestContract
            {
                TurnNumber = 0,
                Modus = GameModus.Backgammon,
                IsWhite = isWhite,
                Board = new BoardModelContract
                {
                    Fields = [15 - bearOffCountBlack - homeBarCountBlack, .. new int[23]],
                    BearOffCountWhite = bearOffCountWhite,
                    BearOffCountBlack = bearOffCountBlack,
                    HomeBarCountWhite = homeBarCountWhite,
                    HomeBarCountBlack = homeBarCountBlack
                }
            };

            var score = await service.EvalBoardStateAsync(new FeatureEvalOptions
            {
                EvalBoardRequest = request,
                ContactWeights = EvalWeights.GetContactWeights(GameModus.Backgammon),
                TurnNumber = request.TurnNumber,
            });

            Assert.Equal(expectedScore, score.Equity);
            neural.Verify(neuralService => neuralService.PredictAsync(
                It.IsAny<NormalizedEvalResultModel>(),
                It.IsAny<IBoardModel>(),
                It.IsAny<bool>()), Times.Never);
        }

        [Theory]
        [InlineData(BotLevel.Hard)]
        [InlineData(BotLevel.Expert)]
        public async Task EvalBoardStateUsesSameTerminalEquityAtEverySearchDepth(BotLevel botLevel)
        {
            var neural = new Mock<INeuralEvalService>();
            var service = new DefaultFeatureEvalService(neural.Object, GameModus.Backgammon);
            var request = new EvalBoardRequestContract
            {
                TurnNumber = 0,
                Modus = GameModus.Backgammon,
                IsWhite = true,
                BotLevel = botLevel,
                Board = new BoardModelContract
                {
                    Fields = [14, .. new int[23]],
                    BearOffCountWhite = 15,
                    HomeBarCountBlack = 1
                }
            };

            var score = await service.EvalBoardStateAsync(new FeatureEvalOptions
            {
                EvalBoardRequest = request,
                ContactWeights = EvalWeights.GetContactWeights(GameModus.Backgammon),
                TurnNumber = request.TurnNumber,
            });

            Assert.Equal(3.0, score.Equity);
            neural.Verify(neuralService => neuralService.PredictAsync(
                It.IsAny<NormalizedEvalResultModel>(),
                It.IsAny<IBoardModel>(),
                It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task EvalBoardStateUsesSameCubelessEquityUnitsAtEverySearchDepth()
        {
            var modus = GameModus.Backgammon;
            var service = new DefaultFeatureEvalService(new FixedPerspectiveNeuralEvalService(), modus);
            var contactWeights = EvalWeights.GetContactWeights(modus);
            var board = new BoardModelContract
            {
                Fields = CreateRaceFields()
            };

            var onePlyScore = await service.EvalBoardStateAsync(new FeatureEvalOptions
            {
                EvalBoardRequest = new EvalBoardRequestContract
                    {
                    TurnNumber = 0,
                        Modus = modus,
                        IsWhite = true,
                        BotLevel = BotLevel.Hard,
                        Board = board
                    },
                ContactWeights = contactWeights,
                TurnNumber = 0,
            });
            var twoPlyScore = await service.EvalBoardStateAsync(new FeatureEvalOptions
            {
                EvalBoardRequest = new EvalBoardRequestContract
                    {
                    TurnNumber = 0,
                        Modus = modus,
                        IsWhite = true,
                        BotLevel = BotLevel.Expert,
                        Board = board
                    },
                ContactWeights = contactWeights,
                TurnNumber = 0,
            });

            Assert.Equal(0.5, onePlyScore.Equity, 6);
            Assert.Equal(onePlyScore.Equity, twoPlyScore.Equity, 6);
            Assert.InRange(twoPlyScore.Equity, -3.0, 3.0);
        }

        [Fact]
        public async Task ExplicitTwoPlyMoveScoreDiffersFromImmediatePositionScore()
        {
            var modus = GameModus.Backgammon;
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard(new BoardModelContract
            {
                Fields = CreateRaceFields(),
                BearOffCountWhite = 0,
                BearOffCountBlack = 0
            });
            var moveSequence = boardService.GetUniqueLegalMoveSequences(board, true, [1, 2]).First();
            var originalContract = board.ToContract(false);
            var immediateBoard = boardService.CreateBoard(new BoardModelContract
            {
                Fields = originalContract.Fields.ToArray(),
                BearOffCountWhite = originalContract.BearOffCountWhite,
                BearOffCountBlack = originalContract.BearOffCountBlack,
                HomeBarCountWhite = originalContract.HomeBarCountWhite,
                HomeBarCountBlack = originalContract.HomeBarCountBlack,
            });
            foreach (var move in moveSequence.Moves)
            {
                boardService.MoveCheckerTo(immediateBoard, move.From, move.To, true);
            }

            var neural = new PipAdvantageNeuralEvalService();
            var service = new DefaultFeatureEvalService(neural, modus);
            var contactWeights = EvalWeights.GetContactWeights(modus);

            var immediateScore = await service.EvalBoardStateAsync(new FeatureEvalOptions
            {
                EvalBoardRequest = new EvalBoardRequestContract
                    {
                    TurnNumber = 0,
                        Modus = modus,
                        IsWhite = true,
                        Board = immediateBoard.ToContract(false),
                        BotLevel = BotLevel.Hard
                    },
                ContactWeights = contactWeights,
                TurnNumber = 0,
            });

            var onePlyScore = (await service.EvalMoveSequenceAsync(new FeatureEvalOptions
            {
                BoardContract = originalContract,
                IsWhite = true,
                Candidate = moveSequence,
                BotLevel = BotLevel.Hard,
                ContactWeights = contactWeights,
                TurnNumber = 0,
            }))
                .Equity;

            var twoPlyScore = (await service.EvalMoveSequenceCandidatesAsync(new FeatureEvalOptions
            {
                BoardContract = originalContract,
                IsWhite = true,
                Candidates = [moveSequence],
                ContactWeights = contactWeights,
                BotLevel = BotLevel.Expert,
                TurnNumber = 0,
            }))
                [0].Equity;

            Assert.Equal(immediateScore.Equity, onePlyScore);
            Assert.NotEqual(immediateScore.Equity, twoPlyScore);
            Assert.Equal(CreateRaceFields(), board.Fields);
            Assert.Equal(0, board.BearOffCountWhite);
            Assert.Equal(0, board.BearOffCountBlack);
        }

        private static FeatureEvalOptions CreateRequest(bool isWhite, int pointsAwayPlayer, int pointsAwayOpp, int cubeValue)
        {
            var request = new EvalCubeRequestContract
            {
                TurnNumber = 0,
                Modus = GameModus.Backgammon,
                IsWhite = isWhite,
                MatchLength = 7,
                PointsAwayPlayer = pointsAwayPlayer,
                PointsAwayOpp = pointsAwayOpp,
                Board = new BoardModelContract
                {
                    Fields = BoardServiceFactory.Create(GameModus.Backgammon).CreateBoard().Fields,
                    DoublingCubeValue = cubeValue
                },
            };
            return new FeatureEvalOptions
            {
                EvalCubeRequest = request,
                IsWhite = isWhite,
                TurnNumber = request.TurnNumber,
            };
        }

        private static int[] CreateRaceFields()
        {
            var fields = new int[24];
            fields[5] = 15;
            fields[18] = -15;
            return fields;
        }

        private sealed class PipAdvantageNeuralEvalService : INeuralEvalService
        {
            public int MaxBatchSize => 32;

            public float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool isWhite) =>
                ExtractFeatures(model, board, isWhite, null);

            public float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool isWhite, float[]? destination)
            {
                destination ??= new float[1];
                Assert.Single(destination);
                destination[0] = isWhite ? board.PipCountBlack - board.PipCountWhite : board.PipCountWhite - board.PipCountBlack;
                return destination;
            }

            public Task<float[][]> PredictBatchAsync(float[][] features) =>
                Task.FromResult(features.Select(row => new[]
                {
                    Math.Clamp(0.5f + row[0] / 200f, 0.01f, 0.99f), 0f, 0f, 0f, 0f
                }).ToArray());

            public Task<float[]> PredictAsync(NormalizedEvalResultModel model, IBoardModel board, bool isWhite)
            {
                var advantage = isWhite
                    ? board.PipCountBlack - board.PipCountWhite
                    : board.PipCountWhite - board.PipCountBlack;
                var winProbability = Math.Clamp(0.5f + (float)advantage / 200f, 0.01f, 0.99f);
                return Task.FromResult(new [] { winProbability, 0f, 0f, 0f, 0f });
            }
        }

        private sealed class FixedPerspectiveNeuralEvalService : INeuralEvalService
        {
            public int MaxBatchSize => 32;

            public float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool isWhite) =>
                ExtractFeatures(model, board, isWhite, null);

            public float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool isWhite, float[]? destination)
            {
                destination ??= new float[1];
                Assert.Single(destination);
                destination[0] = isWhite ? 0.75f : 0.25f;
                return destination;
            }

            public Task<float[][]> PredictBatchAsync(float[][] features) =>
                Task.FromResult(features.Select(row => new[] { row[0], 0f, 0f, 0f, 0f }).ToArray());

            public Task<float[]> PredictAsync(NormalizedEvalResultModel model, IBoardModel board, bool isWhite)
            {
                var winProbability = isWhite ? 0.75f : 0.25f;
                return Task.FromResult(new[] { winProbability, 0f, 0f, 0f, 0f });
            }
        }
    }
}
