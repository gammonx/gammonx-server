using System.Runtime.Serialization;

namespace GammonX.Models.Contracts
{
	/// <summary>
	/// Base class for all REST request payloads.
	/// </summary>
	[DataContract]
	public class ResponseContract<T> where T : ResponsePayload
	{
		[DataMember(Name = "type")]
		public string Type { get; set; }

		[DataMember(Name = "payload")]
		public T Payload { get; set; }

		public ResponseContract(string type, T payload)
		{
			Type = type;
			Payload = payload;
		}
	}
}
