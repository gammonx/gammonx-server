using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public class RequestErrorPayload : RequestPayload
	{
		[DataMember(Name = "code")]
		public string Code { get; private set; }

		[DataMember(Name = "message")]
		public string Message { get; private set; }

		public RequestErrorPayload(string code, string message)
		{
			Code = code;
			Message = message;
		}
	}
}
