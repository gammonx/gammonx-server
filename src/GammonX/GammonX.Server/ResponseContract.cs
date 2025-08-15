using System.Runtime.Serialization;

namespace GammonX.Server
{
	[DataContract]
	public class ResponseContract
	{
		[DataMember(Name = "type", IsRequired = true)]
		public string Type { get; set; }

		[DataMember(Name = "code", IsRequired = false, EmitDefaultValue = false)]
		public string Code { get; set; }

		[DataMember(Name = "message", IsRequired = true)]
		public string Message { get; set; }

		[DataMember(Name = "data", IsRequired = true)]
		public object Data { get; set; }
	}
}
