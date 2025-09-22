using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	/// <summary>
	/// Base class for all event payloads.
	/// </summary>
	[DataContract]
	public class EventResponseContract<T> where T : EventPayloadBase
	{
		[DataMember(Name = "type")]
		public string Type { get; set; }

		[DataMember(Name = "payload")]
		public T Payload { get; set; }

		public EventResponseContract(string type, T payload)
		{
			Type = type;
			Payload = payload;
		}
	}
}
