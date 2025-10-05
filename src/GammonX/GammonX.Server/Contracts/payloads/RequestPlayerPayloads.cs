using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public class RequestPlayerIdPayload : RequestPayload
	{
		[DataMember(Name = "playerId", IsRequired = true)]
		public Guid PlayerId { get; }

		public RequestPlayerIdPayload(Guid playerId)
		{
			PlayerId = playerId;
		}
	}

	[DataContract]
	public class RequestPlayerPayload : RequestPayload
	{
		[DataMember(Name = "player", IsRequired = true)]
		public PlayerContract Player { get; set; }

		public RequestPlayerPayload(PlayerContract contract)
		{
			Player = contract;
		}
	}
}
