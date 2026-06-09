using GammonX.Models.Enums;

using System.Runtime.Serialization;

namespace GammonX.Models.Contracts
{
    [DataContract]
    public class EvalCubeRequestContract
    {
        [DataMember(Name = "modus", IsRequired = true)]
        public GameModus Modus { get; set; }

        [DataMember(Name = "isWhite", IsRequired = true)]
        public bool IsWhite { get; set; }

        [DataMember(Name = "board", IsRequired = true)]
        public BoardModelContract Board { get; set; } = new BoardModelContract();

        /// <summary>
        /// Gets or sets the amount of points the current player needs to win the match
        /// </summary>
        [DataMember(Name = "pointsAwayPlayer", IsRequired = true)]
        public int PointsAwayPlayer { get; set; }

        /// <summary>
        /// Gets or sets the amount of points the opponent needs to win the match
        /// </summary>
        [DataMember(Name = "pointsAwayOpp", IsRequired = true)]
        public int PointsAwayOpp { get; set; }
        
        /// <summary>
        /// Gets or sets the match length.
        /// </summary>
        [DataMember(Name = "matchLength", IsRequired = true)]
        public int MatchLength { get; set; }

        [DataMember(Name = "botLevel", IsRequired = true)]
        public BotLevel BotLevel { get; set; } = BotLevel.Unknown;
    }
}
