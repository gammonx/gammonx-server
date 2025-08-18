using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public sealed class EventErrorPayload : EventPayload
	{
		[DataMember(Name = "code")]
		public string Code { get; set; }

		[DataMember(Name = "message")]	
		public string Message { get; set; }

		public EventErrorPayload(string code, string message, params string[] allowedCommands)
			: base(allowedCommands)
		{
			Code = code;
			Message = message;
		}
	}
}
