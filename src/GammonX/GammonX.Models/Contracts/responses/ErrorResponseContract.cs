using System.Runtime.Serialization;

namespace GammonX.Models.Contracts
{
    [DataContract]
    public sealed class ErrorResponseContract : BaseResponseContract
    {
        [DataMember(Name = "message")]
        public string Message { get; set; } = string.Empty;
    }
}
