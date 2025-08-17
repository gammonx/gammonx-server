using GammonX.Server.Models;

using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	/// <summary>
	/// 
	/// </summary>
	[DataContract]
	public sealed class EventMatchStatePayload : EventPayload
	{
		[DataMember(Name = "matchId")]
		public Guid Id { get; private set; }

		[DataMember(Name = "round")]
		public int Round { get; private set; }

		[DataMember(Name = "matchVariant")]
		public WellKnownMatchVariant Variant { get; private set; }

		[DataMember(Name = "player1")]
		public PlayerContract Player1 { get; set; }

		[DataMember(Name = "player2")]
		public PlayerContract Player2 { get; set; }

		public EventMatchStatePayload(
			Guid id,
			PlayerModel player1,
			PlayerModel player2,
			int round, 
			WellKnownMatchVariant variant)
		{
			Id = id;
			Player1 = player1.ToContract();
			Player2 = player2.ToContract();
			Round = round;
			Variant = variant;
		}
	}
}
