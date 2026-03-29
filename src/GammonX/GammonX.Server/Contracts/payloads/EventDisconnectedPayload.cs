using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
    [DataContract]
    public class EventDisconnectedPayload : EventPayloadBase
    {
        [DataMember(Name = "gracePeriod")]
        public TimeSpan GracePeriod { get; set; }

        [DataMember(Name = "expiration")]
        public DateTime Expiration => DateTime.UtcNow.Add(GracePeriod);

        public EventDisconnectedPayload(TimeSpan gracePeriod)
        {
            GracePeriod = gracePeriod;
        }
    }
}
