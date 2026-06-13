using GammonX.Models.Contracts;

using System.Runtime.Serialization;

namespace GammonX.Server.Bot
{
    [DataContract]
    public class BoardEvalPayload : ResponsePayload
    {
        [DataMember(Name = "evalScore", IsRequired = true)]
        public double EvalScore { get; set; }
    }
}
