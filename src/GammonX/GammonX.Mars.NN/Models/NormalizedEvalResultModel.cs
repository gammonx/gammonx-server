namespace GammonX.Mars.NN.Models
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
        /// Gets the amount of blots for the opponent on the board.
        /// [Range: [0, 1], where 0 means no blots for opponent and 1 means all checkers are blots for opponent.
        /// </summary>
        public double BlotCountOpp { get; init; } = 0;

        /// <summary>
        /// Gets the amount of blots for the player in the start range.
        /// [Range: [0, 1], where 0 means no blots in start range and 1 means 6 blots in start range.
        /// </summary>
        public double BlotInStartRangeCount { get; init; } = 0;

        /// <summary>
        /// Gets the amount of blots for the opponent in the start range.
        /// [Range: [0, 1], where 0 means no blots in start range and 1 means 6 blots in start range.
        /// </summary>
        public double BlotInStartRangeCountOpp { get; init; } = 0;

        /// <summary>
        /// Gets the amount of anchors for the player on the board.
        /// [Range: [0, 1], where 0 means no anchors and 1 means all checkers are anchors.
        /// </summary>
        public double AnchorCount { get; init; } = 0;

        /// <summary>
        /// Gets the amount of anchors for the opponent on the board.
        /// [Range: [0, 1], where 0 means no anchors and 1 means all checkers are anchors.
        /// </summary>
        public double AnchorCountOpp { get; init; } = 0;

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

        /// <summary>
        /// Gets a normalized value indicating the distance the players mother checker is away from home range.
        /// Range: [0, 1], where 0 means the players mother checker is in home range and 1 means it is far away.
        /// </summary>
        public double MotherDistancePlayer { get; init; } = 0;

        /// <summary>
        /// Gets a normalized value indicating the distance the opponents mother checker is away from home range.
        /// Range: [0, 1], where 0 means the opponents mother checker is in home range and 1 means it is far away.
        /// </summary>
        public double MotherDistanceOpp { get; init; } = 0;

        #region Fevga Features

        /// <summary>
        /// Gets the normalized length of the longest prime of the player.
        /// Range: [0, 1], where 0 means no prime and 1 means the longest possible prime.
        /// </summary>
        public double MaxPrimeLengthPlayer { get; init; } = 0;

        /// <summary>
        /// Gets the normalized length of the longest prime of the opponent.
        /// Range: [0, 1], where 0 means no prime and 1 means the longest possible prime.
        /// </summary>
        public double MaxPrimeLengthOpp { get; init; } = 0;

        /// <summary>
        /// Gets the normalized number of checkers of the player on the homebar.
        /// Range: [0, 1], where 0 means no checkers and 1 means all checkers are on the homebar.
        /// </summary>
        public double HomebarCountPlayer { get; init; } = 0;

        /// <summary>
        /// Gets the normalized number of checkers of the opponent on the homebar.
        /// Range: [0, 1], where 0 means no checkers and 1 means all checkers are on the homebar.
        /// </summary>
        public double HomebarCountOpp { get; init; } = 0;

        /// <summary>
        /// Gets the normalized probability of the player to form a prime in his next turn.
        /// Range: [0, 1], where 0 means no probability and 1 means certain prime formation.
        /// </summary>
        public double PrimeProbabilityPlayer { get; init; } = 0.0;

        /// <summary>
        /// Gets the normalized probability of the opponent to form a prime in his next turn.
        /// Range: [0, 1], where 0 means no probability and 1 means certain prime formation.
        /// </summary>
        public double PrimeProbabilityOpp { get; init; } = 0.0;

        /// <summary>
        /// Counts the amount of anchors (blocked points) in front of the opponent.
        /// Range: [0, 1], where 0 means no player anchors in front of opponent and 1 means all player checkers in front of opponent are anchors.
        /// </summary>
        public double AnchorCountInFrontPlayer { get; init; } = 0.0;

        /// <summary>
        /// Counts the amount of anchors (blocked points) in front of the player.
        /// Range: [0, 1], where 0 means no opponent anchors in front of player and 1 means all opponent checkers in front of player are anchors.
        /// </summary>
        public double AnchorCountInFrontOpp { get; init; } = 0.0;

        /// <summary>
        /// Counts the average stack height for the player.
        /// Range: [0, 1], where 0 means all player checkers are alone and 1 means all player checkers are stacked together.
        /// </summary>
        public double AverageStackHeightPlayer { get; init; } = 0.0;

        /// <summary>
        /// Counts the average stack height for the opponent.
        /// Range: [0, 1], where 0 means all opponent checkers are alone and 1 means all opponent checkers are stacked together.
        /// </summary>
        public double AverageStackHeightOpp { get; init; } = 0.0;

        /// <summary>
        /// The average distance to bearoff position on the board for the player.
        /// Range: [0, 1], where 0 means all player checkers are borne off and 1 means all player checkers are on the bar.
        /// </summary>
        public double AverageDistanceToBearOffPlayer { get; init; } = 0.0;

        /// <summary>
        /// The average distance to bearoff position on the board for the opponent.
        /// Range: [0, 1], where 0 means all opponent checkers are borne off and 1 means all opponent checkers are on the bar.
        /// </summary>
        public double AverageDistanceToBearOffOpp { get; init; } = 0.0;

        /// <summary>
        /// Gets the average size of gaps associated with the player.
        /// Range: [0, 1], where 0 means player has no gaps and 1 means player has maximum gap size.
        /// </summary>
        public double AverageGapSizePlayer { get; init; } = 0.0;

        /// <summary>
        /// Gets the average size of gaps for the opponent.
        /// Range: [0, 1], where 0 means opponent has no gaps and 1 means opponent has maximum gap size.
        /// </summary>
        public double AverageGapSizeOpp { get; init; } = 0.0;

        /// <summary>
        /// Gets the number of checkers the player has in the prime zone (e.g. mid board).
        /// Range: [0, 1], where 0 means no player checkers in prime zone and 1 means all player checkers in prime zone.
        /// </summary>
        public double CheckersInPrimeZonePlayer { get; init; } = 0.0;

        /// <summary>
        /// Gets the number of checkers the opponent has in the prime zone (e.g. mid board).
        /// Range: [0, 1], where 0 means no opponent checkers in prime zone and 1 means all opponent checkers in prime zone.
        /// </summary>
        public double CheckersInPrimeZoneOpp { get; init; } = 0.0;

        #endregion Fevga Features

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
                BlotCountOpp = eval.BlotCountOpp / 15,
                BlotInStartRangeCount = eval.BlotInStartRangeCount / 6.0,
                BlotInStartRangeCountOpp = eval.BlotInStartRangeCountOpp / 6.0,
                AnchorCount = eval.AnchorCount / 7,
                AnchorCountOpp = eval.AnchorCountOpp / 7,
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
                PlayerMotherPinned = eval.PlayerMotherPinned,
                MotherDistancePlayer = eval.MotherDistancePlayer / 18.0,
                MotherDistanceOpp = eval.MotherDistanceOpp / 18.0,
                // fevga features
                MaxPrimeLengthPlayer = eval.MaxPrimeLengthPlayer / 6.0,
                MaxPrimeLengthOpp = eval.MaxPrimeLengthOpp / 6.0,
                HomebarCountPlayer = eval.HomebarCountPlayer / 15.0,
                HomebarCountOpp = eval.HomebarCountOpp / 15.0,
                PrimeProbabilityPlayer = eval.PrimeProbabilityPlayer,
                PrimeProbabilityOpp = eval.PrimeProbabilityOpp,
                AnchorCountInFrontPlayer = eval.AnchorCountInFrontPlayer / 15.0,
                AnchorCountInFrontOpp = eval.AnchorCountInFrontOpp / 15.0,
                AverageStackHeightPlayer = eval.AverageStackHeightPlayer / 15.0,
                AverageStackHeightOpp = eval.AverageStackHeightOpp / 15.0,
                AverageDistanceToBearOffPlayer = eval.AverageDistanceToBearOffPlayer / 26.0, // 24 board points + bar + borne off
                AverageDistanceToBearOffOpp = eval.AverageDistanceToBearOffOpp / 26.0, // 24 board points + bar + borne off
                AverageGapSizePlayer = eval.AverageGapSizePlayer / 22.0, // max gap size is when all checkers are alone and spread out
                AverageGapSizeOpp = eval.AverageGapSizeOpp / 22.0, // max gap size is when all checkers are alone and spread out
                CheckersInPrimeZonePlayer = eval.CheckersInPrimeZonePlayer / 15.0,
                CheckersInPrimeZoneOpp = eval.CheckersInPrimeZoneOpp / 15.0,
            };
        }
    }
}
