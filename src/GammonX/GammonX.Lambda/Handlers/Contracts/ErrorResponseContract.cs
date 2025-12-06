using System.Runtime.Serialization;

namespace GammonX.Lambda.Handlers.Contracts
{
    [DataContract]
    public sealed class ErrorResponseContract : BaseResponseContract
    {
        [DataMember(Name = "message")]
        public string Message { get; set; } = string.Empty;
    }
}
