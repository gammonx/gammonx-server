using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public sealed class RequestMatchIdPayload : RequestPayload
	{
		[DataMember(Name = "matchId", IsRequired = true)]
		public Guid MatchId { get; }

		public RequestMatchIdPayload(Guid matchId)
		{
			MatchId = matchId;
		}
	}
}
