namespace GammonX.Mars.Server.Models
{
    public readonly struct NormalizedEvalResultModel
    {
        public NormalizedEvalResultModel()
        {
            // pass
        }

        /// <summary>
        /// Gets boolean indicating if the position is a no contact (race) position 
        /// </summary>
        public bool Race { get; init; }

        /// <summary>
        /// Gets the normalized probability of one player checker being hit on the next roll by opponent.
        /// Range: [0, 1], where 0 means no probability and 1 means certain hit.
        /// </summary>
        public double HitProbability1 { get; init; } = 0.0;

        /// <summary>
        /// Gets the normalized probability of two player checkers being hit on the next roll by opponent.
        /// Range: [0, 1], where 0 means no probability and 1 means 2 certain hits.
        /// </summary>
        public double HitProbability2 { get; init; } = 0.0;

        /// <summary>
        /// Gets the normalized probability of the player pinning at least one opponent checker on the next roll.
        /// Range: [0, 1], where 0 means no probability and 1 means certain hit.
        /// </summary>
        public double HitOpponentProbability1 { get; init; } = 0.0;

        /// <summary>
        /// Gets the normalized probability of the player pinning at least two opponent checkers on the next roll.
        /// Range: [0, 1], where 0 means no probability and 1 means 2 certain hits.
        /// </summary>
        public double HitOpponentProbability2 { get; init; } = 0.0;

        /// <summary>
        /// Gets the amount of blots for the player on the board.
        /// [Range: [0, 1], where 0 means no blots and 1 means all checkers are blots.
        /// </summary>
        public double BlotCount { get; init; } = 0;

        /// <summary>
        /// Gets the amount of blots for the player on the board.
        /// [Range: [0, 1], where 0 means no blots in start range and 1 means 6 blots in start range.
        /// </summary>
        public double BlotInStartRangeCount { get; init; } = 0;

        /// <summary>
        /// Gets the amount of anchors for the player on the board.
        /// [Range: [0, 1], where 0 means no anchors and 1 means all checkers are anchors.
        /// </summary>
        public double AnchorCount { get; init; } = 0;

        /// <summary>
        /// Gets the normalized pipcount difference of the player.
        /// Range: [0, 1], where 0 is max disadvantage, 0 equal pip count and 1 is max advantage.
        /// </summary>
        /// <remarks>
        /// Returns a positive value if the player is ahead and a negative value if the player is behind.
        /// </remarks>
        public double PipDifference { get; init; } = 0;

        /// <summary>
        /// Gets the normalized pipcount to bearoff for the player.
        /// Range: [0, 1], where 0 means all checkers are borne off and 1 means all checkers are on the bar.
        /// </summary>
        public double PipToBearOff { get; init; } = 0;

        /// <summary>
        /// Gets the normalized pipcount to bearoff for opponent.
        /// Range: [0, 1], where 0 means all checkers are borne off and 1 means all checkers are on the bar.
        /// </summary>
        public double PipToBearOffOpp { get; init; } = 0;

        /// <summary>
        /// Gets the normalized number of player checkers in front of the last pinned opponent checker in
        /// Range: [0, 1], where 0 means no checkers in front and 1 means all checkers in front.
        /// the players home board.
        /// </summary>
        public double NumChFrontLastPin { get; init; } = 0;

        /// <summary>
        /// Gets the normalized number of opponent checkers in front of the last pinned player checker in
        /// the opponents home board.
        /// Range: [0, 1], where 0 means no checkers in front and 1 means all checkers in front.
        /// </summary>
        public double NumChFrontLastPinOpp { get; init; } = 0;

        /// <summary>
        /// Gets the normalized probability of at least one player checker escaping from the opponents home board on the next roll.
        /// Range: [0, 1], where 0 means no probability and 1 means certain escape.
        /// </summary>
        public double EscapeProbability1 { get; init; } = 0.0;

        /// <summary>
        /// Gets the normalized probability of at least two player checkers escaping from the opponents home board on the next roll.
        /// Range: [0, 1], where 0 means no probability and 1 means 2 certain escapes.
        /// </summary>
        public double EscapeProbability2 { get; init; } = 0.0;

        /// <summary>
        /// Gets the normalized probability of at least one opponent checker escaping from the players home board on the next roll.
        /// Range: [0, 1], where 0 means no probability and 1 means certain escape.
        /// </summary>
        public double EscapeProbability1Opp { get; init; } = 0.0;

        /// <summary>
        /// Gets the normalized probability of at least two opponent checkers escaping from the players home board on the next roll.
        /// Range: [0, 1], where 0 means no probability and 1 means 2 certain escapes.
        /// </summary>
        public double EscapeProbability2Opp { get; init; } = 0.0;

        /// <summary>
        /// Gets the normalized number of opponent checkers currently pinned by the player.
        /// Range: [0, 1], where 0 means no pinned checkers and 1 means all opponent checkers are pinned.
        /// </summary>
        public double PinCountOpp { get; init; } = 0;

        /// <summary>
        /// Gets the normalized number of player checkers currently pinned by the opponent.
        /// Range: [0, 1], where 0 means no pinned checkers and 1 means all player checkers are pinned.
        /// </summary>
        public double PinCountPlayer { get; init; } = 0;

        /// <summary>
        /// Gets a normalized value indicating whether the opponents mother checker is currently pinned by the player (0 or 1).
        /// Range: [0, 1], where 0 means the opponents mother checker is not pinned and 1 means it is pinned.
        /// </summary>
        public double OppMotherPinned { get; init; } = 0;

        /// <summary>
        /// Gets a normalized value indicating whether the players mother checker is currently pinned by the opponent (0 or 1).
        /// Range: [0, 1], where 0 means the players mother checker is not pinned and 1 means it is pinned.
        /// </summary>
        public double PlayerMotherPinned { get; init; } = 0;

        public static NormalizedEvalResultModel From(EvalResultModel eval)
        {
            return new NormalizedEvalResultModel()
            {
                Race = eval.Race,
                HitProbability1 = eval.HitProbability1,
                HitProbability2 = eval.HitProbability2,
                HitOpponentProbability1 = eval.HitOpponentProbability1,
                HitOpponentProbability2 = eval.HitOpponentProbability2,
                // we use a practical max pip difference than theoretical max and remap boundaries to 0-1
                BlotCount = eval.BlotCount / 15,
                BlotInStartRangeCount = eval.BlotInStartRangeCount / 6.0,
                AnchorCount = eval.AnchorCount / 7,
                PipDifference = (eval.PipDifference / 167.0 + 1.0) / 2.0,
                PipToBearOff = eval.PipToBearOff / 167.0, // we use a practical max pip count than theoretical max
                PipToBearOffOpp = eval.PipToBearOffOpp / 167.0, // we use a practical max pip count than theoretical max
                NumChFrontLastPin = eval.NumChFrontLastPin / 15.0,
                NumChFrontLastPinOpp = eval.NumChFrontLastPinOpp / 15.0,
                EscapeProbability1 = eval.EscapeProbability1,
                EscapeProbability2 = eval.EscapeProbability2,
                EscapeProbability1Opp = eval.EscapeProbability1Opp,
                EscapeProbability2Opp = eval.EscapeProbability2Opp,
                PinCountPlayer = eval.PinCountPlayer / 15.0,
                PinCountOpp = eval.PinCountOpp / 15.0,
                OppMotherPinned = eval.OppMotherPinned,
                PlayerMotherPinned = eval.PlayerMotherPinned
            };
        }
    }
}
