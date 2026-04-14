using GammonX.Models.Contracts;

using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public class RequestPlayerIdPayload : ResponsePayload
	{
		[DataMember(Name = "playerId", IsRequired = true)]
		public Guid PlayerId { get; set; }

		public RequestPlayerIdPayload(Guid playerId)
		{
			PlayerId = playerId;
		}
	}

	[DataContract]
	public class RequestPlayerPayload : ResponsePayload
	{
		[DataMember(Name = "player", IsRequired = true)]
		public PlayerContract Player { get; set; }

		public RequestPlayerPayload(PlayerContract contract)
		{
			Player = contract;
		}
	}
}
