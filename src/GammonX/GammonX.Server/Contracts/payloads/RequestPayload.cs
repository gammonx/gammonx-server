using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	/// <summary>
	/// Marker base class for all request payloads.
	/// </summary>
	[DataContract]
	public abstract class RequestPayload
	{
	}

	[DataContract]
	public class DeleteRequestPayload : RequestPayload
	{
		[DataMember(Name = "deleted", IsRequired = true)]
		public bool Deleted { get; set; }

		public DeleteRequestPayload(bool deleted)
		{
			Deleted = deleted;
		}
	}
}
