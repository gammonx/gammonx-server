using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
    [DataContract]
    public class EventTurnTimerPayload : EventPayloadBase
    {
        [DataMember(Name = "activePlayer")]
        public Guid ActivePlayer { get; set; }

        [DataMember(Name = "turnExpiration")]
        public DateTime TurnExpiration { get; set; }
    }
}
