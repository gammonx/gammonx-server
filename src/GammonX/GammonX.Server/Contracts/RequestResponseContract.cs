using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	/// <summary>
	/// Base class for all REST request payloads.
	/// </summary>
	[DataContract]
	public class RequestResponseContract<T> where T : RequestPayload
	{
		[DataMember(Name = "type")]
		public string Type { get; set; }

		[DataMember(Name = "payload")]
		public T Payload { get; set; }

		public RequestResponseContract(string type, T payload)
		{
			Type = type;
			Payload = payload;
		}
	}
}
