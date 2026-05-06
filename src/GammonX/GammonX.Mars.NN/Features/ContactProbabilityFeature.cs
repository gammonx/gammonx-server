using GammonX.Engine.Models;
using GammonX.Engine.Services;

namespace GammonX.Mars.NN.Features
{
    public readonly record struct ContactProbabilityResult(
        double HitProbability1,
        double HitProbability2,
        double EscapeProbability1,
        double EscapeProbability2
    );

    /// <summary>
    /// Combined evaluator for hit and escape probabilities.
    /// Generates each dice combinations legal move sequences once and extracts both features.
    /// </summary>
    public sealed class ContactProbabilityFeature : IFeature<(ContactProbabilityResult Player, ContactProbabilityResult Opponent)>
    {
        private readonly IBoardService _boardService;

        public ContactProbabilityFeature(IBoardService boardService)
        {
            _boardService = boardService;
        }

        // <inheritdoc />
        public (ContactProbabilityResult Player, ContactProbabilityResult Opponent) Eval(IBoardModel board, bool isWhite)
        {
            // blots the opponent can hit
            var exposedPlayer = GetExposedCheckers(board, isWhite);
            // blots the player can hit
            var exposedOpp = GetExposedCheckers(board, !isWhite);
            var playerInOppHome = HasCheckersInOpponentHome(board, !isWhite);
            var oppInPlayerHome = HasCheckersInOpponentHome(board, isWhite);

            int hitRolls1P = 0, hitRolls2P = 0, escRolls1P = 0, escRolls2P = 0;
            int hitRolls1O = 0, hitRolls2O = 0, escRolls1O = 0, escRolls2O = 0;

            for (int die1 = 1; die1 <= 6; die1++)
            {
                for (int die2 = die1; die2 <= 6; die2++)
                {
                    var weight = die1 == die2 ? 1 : 2;
                    int[] rolls = die1 == die2 ? [die1, die1, die1, die1] : [die1, die2];

                    // we explore opponents moves, used for players hit exposure and opponents escape
                    if (exposedPlayer.Count != 0 || oppInPlayerHome)
                    {
                        bool canHitP1 = false, canHitP2 = false;
                        bool canEscO1 = false, canEscO2 = false;

                        _boardService.ExploreLegalMoveSequences(board, !isWhite, rolls, moves =>
                        {
                            int hits = 0, escs = 0;
                            foreach (var move in moves)
                            {
                                if (exposedPlayer.Contains(move.To)) hits++;
                                if (board.IsInHomeOperator(isWhite, move.From) && !board.IsInHomeOperator(isWhite, move.To)) escs++;
                            }
                            if (hits >= 1) canHitP1 = true;
                            if (hits >= 2) canHitP2 = true;
                            if (escs >= 1) canEscO1 = true;
                            if (escs >= 2) canEscO2 = true;
                            return canHitP1 && canHitP2 && canEscO1 && canEscO2;
                        });

                        if (canHitP1) hitRolls1P += weight;
                        if (canHitP2) hitRolls2P += weight;
                        if (canEscO1) escRolls1O += weight;
                        if (canEscO2) escRolls2O += weight;
                    }

                    // we explore players moves, used for opponents hit exposure and players escape
                    if (exposedOpp.Count != 0 || playerInOppHome)
                    {
                        bool canHitO1 = false, canHitO2 = false;
                        bool canEscP1 = false, canEscP2 = false;

                        _boardService.ExploreLegalMoveSequences(board, isWhite, rolls, moves =>
                        {
                            int hits = 0, escs = 0;
                            foreach (var move in moves)
                            {
                                if (exposedOpp.Contains(move.To)) hits++;
                                if (board.IsInHomeOperator(!isWhite, move.From) && !board.IsInHomeOperator(!isWhite, move.To)) escs++;
                            }
                            if (hits >= 1) canHitO1 = true;
                            if (hits >= 2) canHitO2 = true;
                            if (escs >= 1) canEscP1 = true;
                            if (escs >= 2) canEscP2 = true;
                            return canHitO1 && canHitO2 && canEscP1 && canEscP2;
                        });

                        if (canHitO1) hitRolls1O += weight;
                        if (canHitO2) hitRolls2O += weight;
                        if (canEscP1) escRolls1P += weight;
                        if (canEscP2) escRolls2P += weight;
                    }
                }
            }

            var player = new ContactProbabilityResult(hitRolls1P / 36.0, hitRolls2P / 36.0, escRolls1P / 36.0, escRolls2P / 36.0);
            var opponent = new ContactProbabilityResult(hitRolls1O / 36.0, hitRolls2O / 36.0, escRolls1O / 36.0, escRolls2O / 36.0);
            return (player, opponent);
        }

        private static bool HasCheckersInOpponentHome(IBoardModel board, bool isWhite)
        {
            var fieldTuples = board.Fields.Index();

            if (!isWhite)
            {
                // we want to check if white checkers are in black home
                var whiteIndices = fieldTuples.Where(i => i.Item < 0).Select(i => i.Index);
                return whiteIndices.Any(wi => board.IsInHomeOperator(isWhite, wi));
            }
            else
            {
                // we want to check if black checkers are in white home
                var blackIndices = fieldTuples.Where(i => i.Item > 0).Select(i => i.Index);
                return blackIndices.Any(wi => board.IsInHomeOperator(isWhite, wi));
            }
        }

        private static HashSet<int> GetExposedCheckers(IBoardModel board, bool isWhite)
        {
            if (isWhite)
            {
                return board.Fields.Index()
                .Where(i => i.Item == -1)
                .Select(i => i.Index)
                .ToHashSet();
            }
            else
            {
                return board.Fields.Index()
                .Where(i => i.Item == 1)
                .Select(i => i.Index)
                .ToHashSet();
            }
        }
    }
}