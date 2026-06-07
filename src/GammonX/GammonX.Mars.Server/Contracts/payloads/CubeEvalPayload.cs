using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using System.Runtime.Serialization;

namespace GammonX.Mars.Server.Contracts
{
    [DataContract]
    public class CubeEvalPayload : ResponsePayload
    {
        [DataMember(Name = "cubeAction", IsRequired = true)]
        public CubeAction CubeAction { get; set; }
    }
}
