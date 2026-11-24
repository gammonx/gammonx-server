using GammonX.Engine.Models;
using GammonX.Models.Enums;
using GammonX.Diagnostics.Server;

using GammonX.Engine.Services;

namespace GammonX.Server.Models
{
    /// <summary>
    /// Builds the move list consumed by a player response or a local bot evaluation.
    /// </summary>
    public static class GameSessionExtensions
    {
        /// <summary>
        /// Generates the legal view on an independent copy of the rolled position.
        /// </summary>
        /// <param name="game">Round supplying the board rules, position and remaining dice.</param>
        /// <param name="isWhite">True for white checkers, false for black.</param>
        /// <returns>Detached legal sequences in the original board orientation.</returns>
        public static MoveSequences GetLegalMoveSequences(this IGameSessionModel game, bool isWhite)
        {
            using var measurement = ServerMetrics.Measure(ServerMetrics.MoveGenerationDuration);
            var board = game.BoardModel.CloneForMoveGeneration();
            var sequences = game.BoardService.GetLegalMoveSequences(board, isWhite, game.DiceRolls.GetRemainingRolls());
            ServerMetrics.SequenceCount.Record(sequences.Length);
            return new MoveSequences(sequences);
        }

        internal static MoveSequences ResolveMoveSequences(this IGameSessionModel game, bool white)
        {
            var moves = new MoveSequences();
            var board = game.BoardModel;
            var points = Enumerable.Range(0, board.Fields.Length);
            var rolls = game.DiceRolls.GetRemainingRolls();

            // A combined command moves one checker by a nonempty subset of the
            // remaining dice. Oversized bear-offs have their own destination.
            var distances = new HashSet<int> { 0 };
            foreach (var roll in rolls)
            {
                foreach (var prior in distances.ToArray())
                {
                    distances.Add(prior + roll);
                }
            }

            distances.Remove(0);
            var bearOff = white ? BoardPositions.BearOffWhite : BoardPositions.BearOffBlack;
            points = points.Append(white ? BoardPositions.HomeBarWhite : BoardPositions.HomeBarBlack);

            // we iterate through all possible and well known board indices
            foreach (var from in points)
            {
                if ((uint)from < board.Fields.Length && (white ? board.Fields[from] >= 0 : board.Fields[from] <= 0))
                {
                    continue;
                }

                // we fetch all destinations based on the given from position
                var destinations = distances
                    .Select(distance => board.MoveOperator(white, from, distance))
                    .Where(to => (uint)to < board.Fields.Length).Append(bearOff);

                foreach (var to in destinations)
                {
                    var legalMoveSeq = game.BoardService.ResolveMoveSequence(game.BoardModel, white, from, to, rolls);
                    // we add the move if a legal move sequences could be resolved
                    if (legalMoveSeq != null)
                    {
                        // we return the move wrapped in a "flat" move sequence (may contain combined moves)
                        moves.Add(new MoveSequenceModel { Moves = [new MoveModel(from, to)] });
                    }
                }
            }

            return moves;
        }
    }
}
