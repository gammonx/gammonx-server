using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	/// <summary>
	/// 
	/// </summary>
	[DataContract]
	public sealed class EventErrorPayload : EventPayload
	{
		[DataMember(Name = "code")]
		public string Code { get; private set; }

		[DataMember(Name = "message")]	
		public string Message { get; private set; }

		public EventErrorPayload(string code, string message)
		{
			Code = code;
			Message = message;
		}
	}
}
