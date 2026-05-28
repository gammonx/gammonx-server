using GammonX.Engine.Extensions;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Mars.Training
{
    public sealed record TournamentGameResult(
        bool WhiteWon, 
        int TurnCount,
        bool Discarded);

    public sealed record TournamentResult(
        string ModelALabel,
        string ModelBLabel,
        int TotalGames,
        int ModelAWins,
        int ModelBWins,
        int Draws,
        int Discarded,
        double AvgTurns,
        IReadOnlyList<double> ModelAWinRateHistory);

    public static class TournamentRunner
    {
        public static TournamentResult Run(
            GameModus modus,
            string modelAPath,
            string modelBPath,
            int totalGames,
            ContactWeightModel contactWeights,
            ContactWeightModel cheapContactWeights,
            RaceWeightModel raceWeights)
        {
            var modelALabel = Path.GetFileNameWithoutExtension(modelAPath);
            var modelBLabel = Path.GetFileNameWithoutExtension(modelBPath);

            Console.WriteLine($"Loading model A: {modelAPath}");
            var serviceA = NeuralEvalService.Load(modus, modelAPath);
            Console.WriteLine($"Loading model B: {modelBPath}");
            var serviceB = NeuralEvalService.Load(modus, modelBPath);

            var modelAWins = 0;
            var modelBWins = 0;
            var draws = 0;
            var discarded = 0;
            var totalTurns = 0L;
            var winRateHistory = new List<double>();
            var lockObj = new object();

            Console.WriteLine();
            Console.WriteLine($"Starting tournament: {totalGames} games  modus={modus}");
            Console.WriteLine($"  Model A (white): {modelALabel}");
            Console.WriteLine($"  Model B (black): {modelBLabel}");
            Console.WriteLine();

            Parallel.For(
                0,
                totalGames,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                i =>
                {
                    // we alternate which model plays white to eliminate first-mover bias
                    var modelAIsWhite = i % 2 == 0;
                    var result = PlayGame(modus, serviceA, serviceB, modelAIsWhite, contactWeights, cheapContactWeights, raceWeights);

                    lock (lockObj)
                    {
                        totalTurns += result.TurnCount;

                        if (result.Discarded)
                        {
                            discarded++;
                        }
                        else if (result.WhiteWon)
                        {
                            if (modelAIsWhite) 
                                modelAWins++;
                            else 
                                modelBWins++;
                        }
                        else
                        {
                            // black won or draw
                            if (!modelAIsWhite) 
                                modelAWins++;
                            else if (result.TurnCount < 250) 
                                modelBWins++;
                            else
                                draws++;
                        }

                        var played = modelAWins + modelBWins + draws + discarded;
                        var total = modelAWins + modelBWins;
                        var winRate = total > 0 ? (double)modelAWins / total : 0.5;
                        winRateHistory.Add(winRate);

                        if (played % 50 == 0 || played == totalGames)
                        {
                            var (low, high) = ComputeWilsonCi(modelAWins, total);
                            Console.WriteLine($"  {played,5}/{totalGames}  AWins={modelAWins}  BWins={modelBWins}  Draws={draws}  Discarded={discarded}  " +
                                              $"A-winrate={winRate:P1}  95%CI=[{low:P1},{high:P1}]");
                        }
                    }
                });

            return new TournamentResult(
                modelALabel,
                modelBLabel,
                totalGames,
                modelAWins,
                modelBWins,
                draws,
                discarded,
                totalGames > 0 ? (double)totalTurns / totalGames : 0,
                winRateHistory);
        }

        private static TournamentGameResult PlayGame(
            GameModus modus,
            INeuralEvalService serviceA,
            INeuralEvalService serviceB,
            bool modelAIsWhite,
            ContactWeightModel contactWeights,
            ContactWeightModel cheapContactWeights,
            RaceWeightModel raceWeights)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var evalServiceA = FeatureEvalServiceFactory.Create(modus, serviceA);
            var evalServiceB = FeatureEvalServiceFactory.Create(modus, serviceB);
            var diceService = new DiceServiceFactory().Create(DiceServiceType.Simple);

            var isWhite = Random.Shared.Next(2) == 0;
            const int maxTurns = 250;
            var turnCount = 0;

            while (board.BearOffCountBlack != board.WinConditionCount
                && board.BearOffCountWhite != board.WinConditionCount
                && turnCount < maxTurns)
            {
                turnCount++;
                var rolls = diceService.Roll(2, 6);
                rolls = rolls[0] == rolls[1]
                    ? [rolls[0], rolls[0], rolls[0], rolls[0]]
                    : [rolls[0], rolls[1]];

                var evalRequest = new EvalMoveRequestContract
                {
                    Board = board.ToContract(false),
                    IsWhite = isWhite,
                    Modus = modus,
                    Rolls = rolls
                };

                // we let model A play white on even numbers and model B play white on odd numbers
                // we want to eliminate any first-mover advantage by alternating colors every game
                var activeService = (isWhite == modelAIsWhite) ? evalServiceA : evalServiceB;

                var result = activeService.EvalMoveSequenceForTraining(
                    evalRequest,
                    cheapContactWeights,
                    contactWeights,
                    raceWeights,
                    50);

                if (result.Length != 0)
                {
                    foreach (var move in result[0].Move.Moves)
                    {
                        boardService.MoveCheckerTo(board, move.From, move.To, isWhite);
                    }
                }

                isWhite = !isWhite;

                if (board is IPinModel pinModel && pinModel.BothMothersArePinned)
                    return new TournamentGameResult(false, turnCount, false);
            }

            if (turnCount >= maxTurns)
                return new TournamentGameResult(false, turnCount, true);

            var whiteWon = board.BearOffCountWhite == board.WinConditionCount;
            return new TournamentGameResult(whiteWon, turnCount, false);
        }

        /// <summary>
        /// Wilson score confidence interval for a proportion.
        /// More accurate than normal approximation for small samples or extreme proportions.
        /// </summary>
        private static (double low, double high) ComputeWilsonCi(int successes, int total, double z = 1.96)
        {
            if (total == 0) return (0, 1);
            var p = (double)successes / total;
            var n = total;
            var denom = 1 + z * z / n;
            var centre = (p + z * z / (2 * n)) / denom;
            var margin = z * Math.Sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / denom;
            return (Math.Max(0, centre - margin), Math.Min(1, centre + margin));
        }

        public static void PrintReport(TournamentResult result)
        {
            var decisive = result.ModelAWins + result.ModelBWins;
            var winRate = decisive > 0 ? (double)result.ModelAWins / decisive : 0.5;
            var ci = ComputeWilsonCi(result.ModelAWins, decisive);
            var significance = decisive >= 100
                ? DetermineSignificance(result.ModelAWins, decisive)
                : "insufficient games for significance test";

            Console.WriteLine();
            Console.WriteLine("===========================================");
            Console.WriteLine("  Tournament Results");
            Console.WriteLine("===========================================");
            Console.WriteLine();
            Console.WriteLine($"  Model A : {result.ModelALabel}");
            Console.WriteLine($"  Model B : {result.ModelBLabel}");
            Console.WriteLine($"  Modus   : (from game)");
            Console.WriteLine();
            Console.WriteLine($"  Total games  : {result.TotalGames}");
            Console.WriteLine($"  Decisive     : {decisive}");
            Console.WriteLine($"  Draws        : {result.Draws}");
            Console.WriteLine($"  Discarded    : {result.Discarded}");
            Console.WriteLine($"  Avg turns    : {result.AvgTurns:F1}");
            Console.WriteLine();
            Console.WriteLine($"  Model A wins : {result.ModelAWins} ({(decisive > 0 ? (double)result.ModelAWins / decisive : 0):P1})");
            Console.WriteLine($"  Model B wins : {result.ModelBWins} ({(decisive > 0 ? (double)result.ModelBWins / decisive : 0):P1})");
            Console.WriteLine();
            Console.WriteLine($"  A win rate   : {winRate:P2}");
            Console.WriteLine($"  95% CI       : [{ci.low:P2}, {ci.high:P2}]");
            Console.WriteLine($"  Significance : {significance}");
            Console.WriteLine();

            // convergence of win rate over time
            if (result.ModelAWinRateHistory.Count >= 10)
            {
                var last10 = result.ModelAWinRateHistory.TakeLast(10).ToList();
                var last10Avg = last10.Average();
                var last10Std = Math.Sqrt(last10.Average(x => (x - last10Avg) * (x - last10Avg)));
                Console.WriteLine($"  Win rate last 10 checkpoints: {last10Avg:P2} ± {last10Std:P2}");
            }

            Console.WriteLine();
            PrintVerdict(ci, decisive);
            Console.WriteLine("===========================================");
        }

        private static string DetermineSignificance(int aWins, int total)
        {
            if (total == 0) return "no data";
            var p = (double)aWins / total;

            // two-sided binomial z-test vs H0: p=0.5
            var z = Math.Abs((p - 0.5) / Math.Sqrt(0.25 / total));
            return z switch
            {
                >= 3.29 => $"p<0.001 (z={z:F2}) — highly significant",
                >= 2.58 => $"p<0.01  (z={z:F2}) — significant",
                >= 1.96 => $"p<0.05  (z={z:F2}) — significant",
                >= 1.65 => $"p<0.10  (z={z:F2}) — marginal",
                _ =>       $"p>0.10  (z={z:F2}) — not significant"
            };
        }

        private static void PrintVerdict((double low, double high) ci, int decisive)
        {
            if (decisive < 100)
            {
                Console.WriteLine("  Verdict: Too few games for a reliable conclusion.");
                Console.WriteLine("           Run at least 500 games for statistical significance.");
                return;
            }

            if (ci.low > 0.52)
                Console.WriteLine("  Verdict: A is STRONGER (significant).");
            else if (ci.high < 0.48)
                Console.WriteLine("  Verdict: B is STRONGER (significant).");
            else if (ci.low > 0.48 && ci.high < 0.52)
                Console.WriteLine("  Verdict: EQUIVALENT (within noise).");
            else
                Console.WriteLine("  Verdict: INCONCLUSIVE.");
        }
    }
}
