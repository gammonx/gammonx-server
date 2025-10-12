using GammonX.Server.Models;

using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public sealed class EventMatchStatePayload : EventPayloadBase
	{
		[DataMember(Name = "id")]
		public Guid Id { get; set; }

		[DataMember(Name = "winner", IsRequired = true, EmitDefaultValue = true)]
		public Guid? Winner { get; set; }

		[DataMember(Name = "winnerScore", IsRequired = true, EmitDefaultValue = true)]
		public int? WinnerPoints { get; set; }

		[DataMember(Name = "loser", IsRequired = true, EmitDefaultValue = true)]
		public Guid? Loser { get; set; }

		[DataMember(Name = "loserScore", IsRequired = true, EmitDefaultValue = true)]
		public int? LoserPoints { get; set; }

		[DataMember(Name = "groupName")]
		public string GroupName => $"match_{Id}";

		[DataMember(Name = "gameRound")]
		public int GameRound { get; set; }

		[DataMember(Name = "gameRounds")]
		public GameRoundContract[]? GameRounds { get; set; }

		[DataMember(Name = "variant")]
		public WellKnownMatchVariant Variant { get; set; }

		[DataMember(Name = "modus")]
		public WellKnownMatchModus Modus { get; set; }

		[DataMember(Name = "type")]
		public WellKnownMatchType Type { get; set; }	

		[DataMember(Name = "player1")]
		public PlayerContract? Player1 { get; set; }

		[DataMember(Name = "player2")]
		public PlayerContract? Player2 { get; set; }
	}
}
