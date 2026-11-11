using GammonX.Engine.Extensions;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Bot;
using GammonX.Server.Models;
using GammonX.Server.Services;

using GammonX.Server.Tests.Utils;

using static TorchSharp.torch;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Mars.Training.Tournament
{
    public static class TournamentRunner
    {
        private const int MinimumCompletedGames = 100;

        private const double ConfidenceMultiplier = 1.96;

        public static BotPlayerAssignment AssignBotPlayers(Guid player1Id, Guid player2Id, bool modelIsWhite)
        {
            return modelIsWhite ? new BotPlayerAssignment(player1Id, player2Id) : new BotPlayerAssignment(player2Id, player1Id);
        }

        internal static (TournamentEntry ModelA, TournamentEntry? ModelB) AssignColors(
            TournamentEntry modelA,
            TournamentEntry? modelB,
            bool modelAIsWhite)
        {
            return (
                modelA with { IsWhite = modelAIsWhite },
                modelB is null ? null : modelB with { IsWhite = !modelAIsWhite });
        }

        public static async Task<TournamentResult> RunAsync(
            GameModus modus,
            TournamentEntry modelA,
            TournamentEntry? modelB,
            int totalGames,
            ContactWeightModel contactWeights,
            int evalBatchSize,
            int processCount)
        {
            var modelALabel = Path.GetFileNameWithoutExtension(modelA.Path) ?? "undefined model";
            var modelBLabel = Path.GetFileNameWithoutExtension(modelB?.Path);

            Console.WriteLine($"Loading model A: {modelA.Path}");
            var device = cuda.is_available() ? CUDA : CPU;
            var serviceA = BatchedNeuralEvalService.Load(modus, modelA.Path!, device, evalBatchSize);
            modelA = modelA with { Service = serviceA };
            // we expect the background process to be terminated if the parent process closes
            await ((BatchedNeuralEvalService)serviceA).StartAsync(CancellationToken.None);
            if (!string.IsNullOrEmpty(modelB?.Path))
            {
                Console.WriteLine($"Loading model B: {modelB.Path}");
                var serviceB = BatchedNeuralEvalService.Load(modus, modelB.Path, device, evalBatchSize);
                modelB = modelB with { Service = serviceB };
                // we expect the background process to be terminated if the parent process closes
                await ((BatchedNeuralEvalService)serviceB).StartAsync(CancellationToken.None);
            }

            var score = new TournamentScore();
            var lockObj = new object();

            Console.WriteLine();
            Console.WriteLine($"Starting tournament: {totalGames} games  modus={modus}");
            Console.WriteLine($"  Model A: {modelALabel} BotLevel: {modelA.Level}");
            Console.WriteLine($"  Model B: {modelBLabel ?? "wildbg"} BotLevel: {modelB?.Level.ToString() ?? "none"}");
            Console.WriteLine();

            await Parallel.ForAsync(
                0,
                totalGames,
                new ParallelOptions { MaxDegreeOfParallelism = processCount },
                async (i, _) =>
                {
                    try
                    {
                        // we alternate which model plays white to eliminate first-mover bias
                        var modelAIsWhite = i % 2 == 0;
                        var assignedModels = AssignColors(modelA, modelB, modelAIsWhite);
                        TournamentGameResult? result = null;
                        if (assignedModels.ModelB?.Service != null)
                        {
                            result = await PlayGameAsync(
                                modus,
                                assignedModels.ModelA,
                                assignedModels.ModelB,
                                contactWeights);
                        }
                        else
                        {
                            result = await PlayAgainstBotServiceGameAsync(
                                modus,
                                assignedModels.ModelA,
                                contactWeights);
                        }

                        lock (lockObj)
                        {
                            score.RecordResult(result, modelAIsWhite);
                            var played = score.CompletedGames + score.Discarded;

                            if (played % 50 == 0 || played == totalGames)
                            {
                                var equity = ComputePointEquity(score.CompletedGames, score.ModelATotalPoints, score.ModelASquaredPoints);
                                Console.WriteLine(
                                    $"  {played,5}/{totalGames}  A-points/game={FormatPoints(equity.PointsPerGame)}  " +
                                    $"approx.95%CI={FormatConfidenceInterval(equity)}  Scored={score.CompletedGames}  " +
                                    $"A-winrate={FormatWinRate(score.ModelAWins, score.ModelBWins)}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (lockObj)
                        {
                            score.RecordDiscardedGame();
                        }

                        Console.WriteLine($"  Error during tournament (discarded): {ex.Message}");
                    }
                });

            return new TournamentResult(
                modelALabel,
                modelBLabel ?? "wildbg",
                totalGames,
                score.ModelAWins,
                score.ModelBWins,
                score.Draws,
                score.Discarded,
                totalGames > 0 ? (double)score.TotalTurns / totalGames : 0,
                score.ModelATotalPoints,
                score.ModelASquaredPoints)
            {
                ModelAGammonWins = score.ModelAGammonWins,
                ModelABackgammonWins = score.ModelABackgammonWins,
                ModelBGammonWins = score.ModelBGammonWins,
                ModelBBackgammonWins = score.ModelBBackgammonWins,
            };
        }

        private static async Task<TournamentGameResult> PlayGameAsync(GameModus modus, TournamentEntry entryA, TournamentEntry entryB, ContactWeightModel contactWeights)
        {
            ArgumentNullException.ThrowIfNull(entryA.Service);
            ArgumentNullException.ThrowIfNull(entryB.Service);

            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var evalServiceA = FeatureEvalServiceFactory.Create(modus, entryA.Service);
            var evalServiceB = FeatureEvalServiceFactory.Create(modus, entryB.Service);
            var diceService = new DiceServiceFactory().Create(DiceServiceType.Simple);

            var isWhite = Random.Shared.Next(2) == 0;
            const int maxTurns = 250;
            var turnCount = 0;

            while (board.BearOffCountBlack != board.WinConditionCount && board.BearOffCountWhite != board.WinConditionCount && turnCount < maxTurns)
            {
                turnCount++;
                var rolls = diceService.Roll(2, 6);
                rolls = rolls[0] == rolls[1] ? [rolls[0], rolls[0], rolls[0], rolls[0]] : [rolls[0], rolls[1]];

                // We append the roll event because the turn number depends on it
                boardService.AddRollEventToHistory(board, isWhite, rolls);

                // we let model A play white on even numbers and model B play white on odd numbers
                // we want to eliminate any first-mover advantage by alternating colors every game
                var activeService = (isWhite == entryA.IsWhite) ? evalServiceA : evalServiceB;
                var botLevel = isWhite == entryA.IsWhite ? entryA.Level : entryB.Level;

                var evalRequest = new EvalMoveRequestContract
                {
                    Board = board.ToContract(false),
                    IsWhite = isWhite,
                    Modus = modus,
                    Rolls = rolls,
                    BotLevel = botLevel
                };

                var result = await activeService.EvalMoveSequencesAsync(new FeatureEvalOptions
                {
                    EvalMoveRequest = evalRequest,
                    ContactWeights = contactWeights,
                    TurnNumber = turnCount,
                }, CancellationToken.None);

                foreach (var move in result.Moves)
                {
                    boardService.MoveCheckerTo(board, move.From, move.To, isWhite);
                }

                isWhite = !isWhite;

                if (board is IPinModel pinModel && pinModel.BothMothersArePinned)
                {
                    return CreateGameResult(board, turnCount);
                }
            }

            return CreateGameResult(board, turnCount);
        }

        private static async Task<TournamentGameResult> PlayAgainstBotServiceGameAsync(GameModus modus, TournamentEntry modelA, ContactWeightModel contactWeights)
        {
            ArgumentNullException.ThrowIfNull(modelA.Service);
            ArgumentNullException.ThrowIfNull(modelA.IsWhite);

            var diceFactory = new DiceServiceFactory();
            var gameSessionFactory = new GameSessionFactory(diceFactory);
            var matchFactory = new MatchSessionFactory(gameSessionFactory);
            var matchSession = SessionUtils.CreateMatchSessionWithTwoBots(modus.From(), MatchType.CashGame, matchFactory);

            // eval service to test
            var evalService = FeatureEvalServiceFactory.Create(modus, modelA.Service);
            // bot service to play against
            var wildBgService = BotUtils.GetBotService(WellKnownBotServices.WildBg);

            matchSession.Player1.AcceptNextGame();
            matchSession.Player2.AcceptNextGame();

            var assignment = AssignBotPlayers(
                matchSession.Player1.Id,
                matchSession.Player2.Id,
                modelA.IsWhite.Value);
            var evalPlayerId = assignment.ModelPlayerId;
            var wildbgPlayerId = assignment.WildBgPlayerId;

            var activePlayerId = Guid.Empty;
            var otherPlayerId = Guid.Empty;

            if (modelA.IsWhite.Value)
            {
                matchSession.StartMatch(evalPlayerId);
                activePlayerId = evalPlayerId;
                otherPlayerId = wildbgPlayerId;
            }
            else
            {
                matchSession.StartMatch(wildbgPlayerId);
                activePlayerId = wildbgPlayerId;
                otherPlayerId = evalPlayerId;
            }

            const int maxTurns = 250;
            var turnCount = 0;

            var gameSession = matchSession.GetGameSession(1);
            var board = gameSession!.BoardModel;

            do
            {
                turnCount++;
                if (gameSession.Phase == GamePhase.WaitingForRoll)
                {
                    matchSession.RollDices(activePlayerId);
                }

                MoveSequenceModel nextMoves;
                if (activePlayerId == wildbgPlayerId)
                {
                    // wildbg turn
                    nextMoves = await wildBgService.GetNextMovesAsync(matchSession, activePlayerId, CancellationToken.None);
                }
                else
                {
                    // eval service turn
                    var isWhite = modelA.IsWhite.Value;
                    var rolls = gameSession.DiceRolls.Select(dr => dr.Roll).ToArray();
                    var evalRequest = new EvalMoveRequestContract
                    {
                        Board = board.ToContract(false),
                        IsWhite = isWhite,
                        Modus = modus,
                        Rolls = rolls,
                        BotLevel = modelA.Level
                    };
                    nextMoves = await evalService.EvalMoveSequencesAsync(new FeatureEvalOptions
                    {
                        EvalMoveRequest = evalRequest,
                        ContactWeights = contactWeights,
                        TurnNumber = turnCount,
                    }, CancellationToken.None);
                }

                var hasWon = false;
                foreach (var nextMove in nextMoves.Moves)
                {
                    hasWon = matchSession.MoveCheckers(activePlayerId, nextMove.From, nextMove.To);
                    if (hasWon)
                        break;
                }

                if (!hasWon)
                {
                    matchSession.EndTurn(activePlayerId);
                    activePlayerId = otherPlayerId;
                    otherPlayerId = activePlayerId == evalPlayerId ? wildbgPlayerId : evalPlayerId;
                }
                else
                {
                    // we only support the first game of a match (cash game)
                    break;
                }
            }
            while (turnCount < maxTurns);

            return CreateGameResult(board, turnCount);
        }

        internal static TournamentGameResult CreateGameResult(IBoardModel board, int turnCount)
        {
            if (board is IPinModel pinModel && pinModel.BothMothersArePinned)
            {
                return new TournamentGameResult(GameResult.Draw, turnCount, false);
            }

            var whiteWon = board.BearOffCountWhite == board.WinConditionCount;
            var blackWon = board.BearOffCountBlack == board.WinConditionCount;
            if (!whiteWon && !blackWon)
            {
                return new TournamentGameResult(GameResult.Unknown, turnCount, true);
            }

            // Resolve the terminal board before discarding a game at the turn limit.
            var result = board.ToGameResult(Guid.Empty, whiteWon);
            var whiteResult = whiteWon ? result.WinnerResult : result.LoserResult;
            return new TournamentGameResult(whiteResult, turnCount, false);
        }

        internal static int GetModelAPoints(GameResult whiteResult, bool modelAIsWhite)
        {
            // Outcome types give cubeless points; GameResultModel.Points can include the cube.
            var whitePoints = whiteResult switch
            {
                GameResult.Single => 1,
                GameResult.Gammon => 2,
                GameResult.Backgammon => 3,
                GameResult.LostSingle => -1,
                GameResult.LostGammon => -2,
                GameResult.LostBackgammon => -3,
                GameResult.Draw => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(whiteResult), whiteResult, "Unsupported tournament outcome.")
            };

            // Every game is stored from White's perspective; score it for A exactly once.
            return modelAIsWhite ? whitePoints : -whitePoints;
        }

        private static MatchVariant From(this GameModus modus)
        {
            switch (modus)
            {
                case GameModus.Backgammon:
                    return MatchVariant.Backgammon;
                case GameModus.Fevga:
                case GameModus.Plakoto:
                case GameModus.Portes:
                    return MatchVariant.Tavli;
                case GameModus.Tavla:
                    return MatchVariant.Tavla;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modus), modus, null);
            }
        }

        internal static TournamentEquityStatistics ComputePointEquity(int completedGames, long totalPoints, long squaredPoints)
        {
            if (completedGames == 0)
            {
                return new TournamentEquityStatistics(null, null, null);
            }

            var mean = (double)totalPoints / completedGames;
            if (completedGames < MinimumCompletedGames)
            {
                return new TournamentEquityStatistics(mean, null, null);
            }

            // Use unbiased sample variance, including zero-point draws in the sample count.
            // Clamp roundoff; identical observed scores do not establish a zero population variance.
            var variance = Math.Max(0d, (squaredPoints - completedGames * mean * mean) / (completedGames - 1));
            if (variance == 0d)
            {
                return new TournamentEquityStatistics(mean, null, null);
            }

            // Large-sample normal approximation for independent games at a fixed stopping point.
            var margin = ConfidenceMultiplier * Math.Sqrt(variance / completedGames);
            
            return new TournamentEquityStatistics(mean, mean - margin, mean + margin);
        }

        public static void PrintReport(TournamentResult result)
        {
            var decisive = result.ModelAWins + result.ModelBWins;
            var equity = ComputePointEquity(result.CompletedGames, result.ModelATotalPoints, result.ModelASquaredPoints);

            Console.WriteLine();
            Console.WriteLine($"### {result.ModelALabel} vs. {result.ModelBLabel}");
            Console.WriteLine($"  Model A                : {result.ModelALabel}");
            Console.WriteLine($"  Model B                : {result.ModelBLabel}");
            Console.WriteLine("   Modus                  : (from game)");
            Console.WriteLine($"  Total games            : {result.TotalGames}");
            Console.WriteLine($"  Scored games           : {result.CompletedGames}");
            Console.WriteLine($"  Decisive               : {decisive}");
            Console.WriteLine($"  Draws                  : {result.Draws}");
            Console.WriteLine($"  Discarded              : {result.Discarded}");
            Console.WriteLine($"  Avg turns              : {result.AvgTurns:F1}");
            Console.WriteLine($"  A cubeless points/game : {FormatPoints(equity.PointsPerGame)}");
            Console.WriteLine($"  Approx. 95% equity CI  : {FormatConfidenceInterval(equity)}");
            Console.WriteLine($"  Model A wins           : {result.ModelAWins}");
            Console.WriteLine($"  A gammon wins          : {result.ModelAGammonWins}");
            Console.WriteLine($"  A backgammon wins      : {result.ModelABackgammonWins}");
            Console.WriteLine($"  Model B wins           : {result.ModelBWins}");
            Console.WriteLine($"  B gammon wins          : {result.ModelBGammonWins}");
            Console.WriteLine($"  B backgammon wins      : {result.ModelBBackgammonWins}");
            Console.WriteLine($"  A win rate             : {FormatWinRate(result.ModelAWins, result.ModelBWins)}");
            Console.WriteLine($"  Verdict: {DetermineVerdict(result, equity)}");
        }

        private static string FormatPoints(double? points)
        {
            return points?.ToString("+0.0000;-0.0000;0.0000") ?? "n/a";
        }

        private static string FormatConfidenceInterval(TournamentEquityStatistics equity)
        {
            return equity.LowerBound.HasValue && equity.UpperBound.HasValue
                ? $"[{FormatPoints(equity.LowerBound)}, {FormatPoints(equity.UpperBound)}]"
                : "n/a";
        }

        private static string FormatWinRate(int modelAWins, int modelBWins)
        {
            var decisive = modelAWins + modelBWins;
            return decisive > 0 ? ((double)modelAWins / decisive).ToString("P2") : "n/a";
        }

        internal static string DetermineVerdict(TournamentResult result, TournamentEquityStatistics equity)
        {
            if (result.Discarded > 0 || result.CompletedGames != result.TotalGames)
                return "INCONCLUSIVE (discarded or unprocessed games; completed-game scores are diagnostic only).";

            if (result.CompletedGames < MinimumCompletedGames)
                return $"INCONCLUSIVE (at least {MinimumCompletedGames} completed games required for the approximate equity interval).";

            if (!equity.LowerBound.HasValue || !equity.UpperBound.HasValue)
                return "INCONCLUSIVE (insufficient observed variation to estimate an equity interval).";

            if (equity.LowerBound.Value > 0d)
                return "A is STRONGER (positive point equity; approximate 95% CI excludes zero).";

            if (equity.UpperBound.Value < 0d)
                return "B is STRONGER (negative point equity for A; approximate 95% CI excludes zero).";

            return "INCONCLUSIVE (insufficient evidence of a difference in point equity).";
        }
    }
}
