using GammonX.Engine.Models;
using GammonX.Models.Contracts;

using System.Runtime.Serialization;

namespace GammonX.Mars.Server.Contracts
{
    [DataContract]
    public class MoveEvalPayload : ResponsePayload
    {
        [DataMember(Name = "moveSequence", IsRequired = true)]
        public required MoveSequenceModel MoveSequence { get; set; }
    }
}
