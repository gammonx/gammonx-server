using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	/// <summary>
	/// 
	/// </summary>
	[DataContract]
	public class EventResponseContract
	{
		[DataMember(Name = "type")]
		public string Type { get; set; }

		[DataMember(Name = "payload")]
		public EventPayload Payload { get; set; }

		public EventResponseContract(string type, EventPayload payload)
		{
			Type = type;
			Payload = payload;
		}
	}
}
