using GammonX.Server.Models;

using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	/// <summary>
	/// 
	/// </summary>
	[DataContract]
	public sealed class MatchStatePayload : EventPayload
	{
		[DataMember(Name = "matchId")]
		public Guid Id { get; private set; }

		[DataMember(Name = "round")]
		public int Round { get; private set; }

		[DataMember(Name = "matchVariant")]
		public WellKnownMatchVariant Variant { get; private set; }

		[DataMember(Name = "score")]
		public Dictionary<Guid, int> Score { get; private set; } = new();

		public MatchStatePayload(Guid id, int round, WellKnownMatchVariant variant)
		{
			Id = id;
			Round = round;
			Variant = variant;
		}
	}
}
