using GammonX.Server.Models;

using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
    [DataContract]
    public class EventDisconnectedPayload : EventPayloadBase
    {
        [DataMember(Name = "gracePeriod")]
        public TimeSpan GracePeriod { get; set; }

        [DataMember(Name = "expiration")]
        public DateTime Expiration { get; set; }

        [DataMember(Name = "playerId")]
        public Guid PlayerId { get; set; }

        public static EventDisconnectedPayload From(PlayerConnection playerConnection)
        {
            return new EventDisconnectedPayload
            {
                PlayerId = playerConnection.Id,
                GracePeriod = playerConnection.DisconnectGracePeriod,
                Expiration = DateTime.UtcNow.Add(playerConnection.DisconnectGracePeriod)
            };
        }
    }
}
