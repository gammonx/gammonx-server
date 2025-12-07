using GammonX.Models.Enums;

namespace GammonX.Server.Models
{
    /// <summary>
    /// Provides the information on how a game concluded for the winning and losing player.
    /// </summary>
    public struct GameResultModel
    {
        /// <summary>
        /// Gets the player id of the winner.
        /// </summary>
        public Guid WinnerId { get; private set; }

        /// <summary>
        /// Gets the game result of the winning player.
        /// </summary>
        public GameResult WinnerResult { get; private set; }

        /// <summary>
        /// Gets the game result of the losing player.
        /// </summary>
        public GameResult LoserResult { get; private set; }

        /// <summary>
        /// Gets the points awarded to the winning player.
        /// </summary>
        public int Points { get; private set; }

        /// <summary>
        /// Gets a boolean indicating if the game result is not yet concluded.
        /// </summary>
        public readonly bool IsConcluded => !Equals(Empty());

        /// <summary>
        /// Gets a boolean indicating that the game ended in a draw.
        /// </summary>
        public readonly bool IsDraw => Equals(Draw());

        public GameResultModel(Guid winnerId, GameResult winner, GameResult loser, int points)
        {
            WinnerId = winnerId;
            LoserResult = loser;
            WinnerResult = winner;
            Points = points;
        }

        /// <summary>
        /// Gets the <see cref="GameResult"/> for the given <paramref name="playerId"/>.
        /// </summary>
        /// <param name="playerId">Player id.</param>
        /// <returns>Returns the game result.</returns>
        public GameResult GetResult(Guid playerId)
        {
            if (WinnerId.Equals(playerId))
                return WinnerResult;
            return LoserResult;
        }

        public static GameResultModel Draw()
        {
            return new GameResultModel(Guid.Empty, GameResult.Draw, GameResult.Draw, 0);
        }

        public static GameResultModel Empty()
        {
            return new GameResultModel(Guid.Empty, GameResult.Unknown, GameResult.Unknown, 0);
        }
    }
}
