using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public sealed class EventErrorPayload : EventPayloadBase
	{
		[DataMember(Name = "code")]
		public string Code { get; set; }

		[DataMember(Name = "message")]	
		public string Message { get; set; }

		[DataMember(Name = "stackTrace")]
		public string StackTrace { get; set; } = string.Empty;

        public EventErrorPayload(string code, string message, Exception? exception, params string[] allowedCommands)
			: base(allowedCommands)
		{
			Code = code;
			Message = message;
			StackTrace = exception?.StackTrace ?? string.Empty;
        }
	}
}
