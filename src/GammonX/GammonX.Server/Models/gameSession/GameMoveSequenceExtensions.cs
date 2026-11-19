using GammonX.Diagnostics.Server;
using GammonX.Engine.Services;

namespace GammonX.Server.Models
{
    /// <summary>Builds the move list consumed by a player response or a local bot evaluation.</summary>
    public static class GameMoveSequenceExtensions
    {
        /// <summary>Generates the legal view on an independent copy of the rolled position.</summary>
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
    }
}
