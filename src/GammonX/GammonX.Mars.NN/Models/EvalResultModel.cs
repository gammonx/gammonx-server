namespace GammonX.Mars.NN.Models
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
        /// Gets the amount of blots for the player on the board.
        /// </summary>
        public double BlotCount { get; init; } = 0;

        /// <summary>
        /// Gets the amount of blots for the opponent on the board.
        /// </summary>
        public double BlotCountOpp { get; init; } = 0;

        /// <summary>
        /// Gets the amount of blots for the player in his own start range.
        /// </summary>
        public double BlotInStartRangeCount { get; init; } = 0;

        /// <summary>
        /// Gets the amount of blots for the opponent in the start range.
        /// </summary>
        public double BlotInStartRangeCountOpp { get; init; } = 0;

        /// <summary>
        /// Gets the amount of anchors for the player on the board.
        /// </summary>
        public double AnchorCount { get; init; } = 0;

        /// <summary>
        /// Gets the amount of anchors for the opponent on the board.
        /// </summary>
        public double AnchorCountOpp { get; init; } = 0;

        /// <summary>
        /// Gets the pipcount difference of the player.
        /// </summary>
        /// <remarks>
        /// Returns a positive value if the player is ahead and a negative value if the player is behind.
        /// </remarks>
        public double PipDifference { get; init; } = 0;

        /// <summary>
        /// Gets the pipcount to bearoff for the player.
        /// </summary>
        public double PipToBearOff { get; init; } = 0;

        /// <summary>
        /// Gets the pipcount to bearoff for opponent.
        /// </summary>
        public double PipToBearOffOpp { get; init; } = 0;

        /// <summary>
        /// Gets the number of player checkers in front of the last pinned opponent checker in
        /// the players home board.
        /// </summary>
        public double NumChFrontLastPin { get; init; } = 0;

        /// <summary>
        /// Gets the number of opponent checkers in front of the last pinned player checker in
        /// the opponents home board.
        /// </summary>
        public double NumChFrontLastPinOpp { get; init; } = 0;

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
        public double PinCountOpp { get; init; } = 0;

        /// <summary>
        /// Gets the number of player checkers currently pinned by the opponent.
        /// </summary>
        public double PinCountPlayer { get; init; } = 0;

        /// <summary>
        /// Gets a value indicating whether the opponent's mother checker is currently pinned by the player (0 or 1).
        /// </summary>
        public double OppMotherPinned { get; init; } = 0;

        /// <summary>
        /// Gets a value indicating whether the player's mother checker is currently pinned by the opponent (0 or 1).
        /// </summary>
        public double PlayerMotherPinned { get; init; } = 0;

        /// <summary>
        /// Gets a value indicating the distance the players mother checker is away from home range.
        /// </summary>
        public double MotherDistancePlayer { get; init; } = 0;

        /// <summary>
        /// Gets a value indicating the distance the opponents mother checker is away from home range.
        /// </summary>
        public double MotherDistanceOpp { get; init; } = 0;

        #region Fevga Features

        /// <summary>
        /// Gets the length of the longest prime of the player.
        /// </summary>
        public double MaxPrimeLengthPlayer { get; init; } = 0;

        /// <summary>
        /// Gets the length of the longest prime of the opponent.
        /// </summary>
        public double MaxPrimeLengthOpp { get; init; } = 0;

        /// <summary>
        /// Gets the number of checkers of the player on the homebar.
        /// </summary>
        public double HomebarCountPlayer { get; init; } = 0;

        /// <summary>
        /// Gets the number of checkers of the opponent on the homebar.
        /// </summary>
        public double HomebarCountOpp { get; init; } = 0;

        /// <summary>
        /// Gets the probability of the player to form a prime in his next turn.
        /// </summary>
        public double PrimeProbabilityPlayer { get; init; } = 0.0;

        /// <summary>
        /// Gets the probability of the opponent to form a prime in his next turn.
        /// </summary>
        public double PrimeProbabilityOpp { get; init; } = 0.0;

        /// <summary>
        /// Counts the amount of anchors (blocked points) in front of the opponent.
        /// </summary>
        public double AnchorCountInFrontPlayer { get; init; } = 0.0;

        /// <summary>
        /// Counts the amount of anchors (blocked points) in front of the player.
        /// </summary>
        public double AnchorCountInFrontOpp { get; init; } = 0.0;

        /// <summary>
        /// Counts the average stack height for the player.
        /// </summary>
        public double AverageStackHeightPlayer { get; init; } = 0.0;

        /// <summary>
        /// Counts the average stack height for the opponent.
        /// </summary>
        public double AverageStackHeightOpp { get; init; } = 0.0;

        /// <summary>
        /// The average distance to bearoff position on the board for the player.
        /// </summary>
        public double AverageDistanceToBearOffPlayer { get; init; } = 0.0;

        /// <summary>
        /// The average distance to bearoff position on the board for the opponent.
        /// </summary>
        public double AverageDistanceToBearOffOpp { get;  init; } = 0.0;

        /// <summary>
        /// Gets the average size of gaps associated with the player.
        /// </summary>
        public double AverageGapSizePlayer { get; init; } = 0.0;

        /// <summary>
        /// Gets the average size of gaps for the opponent.
        /// </summary>
        public double AverageGapSizeOpp { get; init; } = 0.0;

        /// <summary>
        /// Gets the number of checkers the player has in the prime zone (e.g. mid board).
        /// </summary>
        public double CheckersInPrimeZonePlayer { get; init; } = 0.0;

        /// <summary>
        /// Gets the number of checkers the opponent has in the prime zone (e.g. mid board).
        /// </summary>
        public double CheckersInPrimeZoneOpp { get; init; } = 0.0;

        #endregion Fevga Features
    }
}
