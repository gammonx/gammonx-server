using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using System.Runtime.Serialization;

namespace GammonX.Server.Bot
{
    [DataContract]
    public class CubeEvalPayload : ResponsePayload
    {
        [DataMember(Name = "shouldOffer", IsRequired = true)]
        public CubeAction ShouldOffer { get; set; }

        [DataMember(Name = "shouldTake", IsRequired = true)]
        public CubeAction ShouldTake { get; set; }
    }
}
