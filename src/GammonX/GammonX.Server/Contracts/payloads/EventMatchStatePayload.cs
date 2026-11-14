using GammonX.Models.Enums;

using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public sealed class EventMatchStatePayload : EventPayloadBase
	{
		[DataMember(Name = "id", IsRequired = true)]
		public Guid Id { get; set; }

		[DataMember(Name = "winner", IsRequired = false, EmitDefaultValue = true)]
		public Guid? Winner { get; set; }

		[DataMember(Name = "winnerPoints", IsRequired = false, EmitDefaultValue = true)]
		public int? WinnerPoints { get; set; }

		[DataMember(Name = "loser", IsRequired = false, EmitDefaultValue = true)]
		public Guid? Loser { get; set; }

		[DataMember(Name = "loserPoints", IsRequired = false, EmitDefaultValue = true)]
		public int? LoserPoints { get; set; }

		[DataMember(Name = "groupName", IsRequired = true)]
		public string GroupName => $"match_{Id}";

		[DataMember(Name = "gameRound", IsRequired = true)]
		public int GameRound { get; set; }

		[DataMember(Name = "gameRounds", IsRequired = true)]
		public GameRoundContract[]? GameRounds { get; set; }

		[DataMember(Name = "variant", IsRequired = true)]
		public MatchVariant Variant { get; set; }

		[DataMember(Name = "modus", IsRequired = true)]
		public MatchModus Modus { get; set; }

		[DataMember(Name = "type", IsRequired = true)]
		public GammonX.Models.Enums.MatchType Type { get; set; }	

		[DataMember(Name = "player1", IsRequired = true)]
		public PlayerContract? Player1 { get; set; }

		[DataMember(Name = "player2", IsRequired = true)]
		public PlayerContract? Player2 { get; set; }

		/// <summary>
		/// Gets or sets the relative API URL for comparing the current rating to this match's pre-match rating.
		/// </summary>
        /// <remarks>
        /// Present for Ranked match-end events. The endpoint reads stored data; the client can retry
        /// after a delay while PreviousRating and RatingChange are null because processing is pending.
        /// The comparison includes later matches if they have already updated the current rating.
        /// </remarks>
        [DataMember(Name = "ratingUrl", IsRequired = false, EmitDefaultValue = false)]
        public string? RatingUrl { get; set; }

		/// <summary>
		/// Appends a rating comparison URL for this player when a Ranked match ends.
		/// </summary>
		/// <param name="playerId">Player whose current and pre-match ratings will be compared.</param>
		/// <param name="serverEventType">Match state payload type.</param>
		public void AppendRatingUrl(Guid playerId, string serverEventType)
		{
			if (serverEventType.Equals(ServerEventTypes.MatchEndedEvent) && Modus == MatchModus.Ranked)
			{
				// This match's stored period supplies the pre-match baseline; the request does not update ratings.
				RatingUrl = $"players/{playerId}/rating/{Variant}/{Type}?matchId={Id}";
			}
		}
    }
}
