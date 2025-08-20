using GammonX.Server.Models;

using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public sealed class EventMatchStatePayload : EventPayload
	{
		[DataMember(Name = "id")]
		public Guid Id { get; set; }

		[DataMember(Name = "gameRound")]
		public int GameRound { get; set; }

		[DataMember(Name = "variant")]
		public WellKnownMatchVariant Variant { get; set; }

		[DataMember(Name = "player1")]
		public PlayerContract Player1 { get; set; }

		[DataMember(Name = "player2")]
		public PlayerContract Player2 { get; set; }
	}
}
