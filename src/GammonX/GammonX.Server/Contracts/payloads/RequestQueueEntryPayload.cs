using GammonX.Server.Models;

using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public sealed class RequestQueueEntryPayload : RequestPayload
	{
		/// <summary>
		/// Gets or sets the unique id of this queue entry.
		/// </summary>
		[DataMember(Name = "queueId", IsRequired = true, EmitDefaultValue = true)]
		public Guid? QueueId { get; set; }

		/// <summary>
		/// Gets or sets the match lobby status whether the match lobby is ready togo.
		/// </summary>
		[DataMember(Name = "status", IsRequired = true)]
		public QueueEntryStatus Status { get; set; }

		/// <summary>
		/// Gets or sets the match id if an opponent was found and the lobby created.
		/// </summary>
		[DataMember(Name = "matchId", IsRequired = true, EmitDefaultValue = true)]
		public Guid? MatchId { get; set; }
	}
}
