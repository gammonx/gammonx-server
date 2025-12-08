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
	}
}
