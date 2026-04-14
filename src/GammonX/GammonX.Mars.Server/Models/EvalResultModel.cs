namespace GammonX.Mars.Server.Models
{
    public readonly struct EvalResultModel
    {
        public EvalResultModel()
        {
            // pass
        }

        /// <summary>
        /// Gets boolean indicating if the position is a no contact (race) position 
        /// </summary>
        public bool Race { get; init; }

        /// <summary>
        /// Gets the probability of one player checker being hit on the next roll by opponent.
        /// </summary>
        public double HitProbability1 { get; init; } = 0.0;

        /// <summary>
        /// Gets the probability of two player checkers being hit on the next roll by opponent.
        /// </summary>
        public double HitProbability2 { get; init; } = 0.0;

        /// <summary>
        /// Gets the probability of the player pinning at least one opponent checker on the next roll.
        /// </summary>
        public double HitOpponentProbability1 { get; init; } = 0.0;

        /// <summary>
        /// Gets the probability of the player pinning at least two opponent checkers on the next roll.
        /// </summary>
        public double HitOpponentProbability2 { get; init; } = 0.0;

        /// <summary>
        /// Gets the pipcount difference of the player.
        /// </summary>
        /// <remarks>
        /// Returns a positive value if the player is ahead and a negative value if the player is behind.
        /// </remarks>
        public int PipDifference { get; init; } = 0;

        /// <summary>
        /// Gets the pipcount to bearoff for the player.
        /// </summary>
        public int PipToBearOff { get; init; } = 0;

        /// <summary>
        /// Gets the pipcount to bearoff for opponent.
        /// </summary>
        public int PipToBearOffOpp { get; init; } = 0;

        /// <summary>
        /// Gets the number of player checkers in front of the last pinned opponent checker in
        /// the players home board.
        /// </summary>
        public int NumChFrontLastPin { get; init; } = 0;

        /// <summary>
        /// Gets the number of opponent checkers in front of the last pinned player checker in
        /// the opponents home board.
        /// </summary>
        public int NumChFrontLastPinOpp { get; init; } = 0;

        /// <summary>
        /// Gets the probability of at least one player checker escaping from the opponents home board on the next roll.
        /// </summary>
        public double EscapeProbability1 { get; init; } = 0.0;

        /// <summary>
        /// Gets the probability of at least two player checkers escaping from the opponents home board on the next roll.
        /// </summary>
        public double EscapeProbability2 { get; init; } = 0.0;

        /// <summary>
        /// Gets the probability of at least one opponent checker escaping from the players home board on the next roll.
        /// </summary>
        public double EscapeProbability1Opp { get; init; } = 0.0;

        /// <summary>
        /// Gets the probability of at least two opponent checkers escaping from the players home board on the next roll.
        /// </summary>
        public double EscapeProbability2Opp { get; init; } = 0.0;

        /// <summary>
        /// Gets the number of opponent checkers currently pinned by the player.
        /// </summary>
        public int PinCountOpp { get; init; } = 0;

        /// <summary>
        /// Gets the number of player checkers currently pinned by the opponent.
        /// </summary>
        public int PinCountPlayer { get; init; } = 0;

        /// <summary>
        /// Gets a value indicating whether the opponent's mother checker is currently pinned by the player (0 or 1).
        /// </summary>
        public int OppMotherPinned { get; init; } = 0;

        /// <summary>
        /// Gets a value indicating whether the player's mother checker is currently pinned by the opponent (0 or 1).
        /// </summary>
        public int PlayerMotherPinned { get; init; } = 0;
    }
}
