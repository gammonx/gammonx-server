using GammonX.Server.Models;

using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
    [DataContract]
    public class EventDisconnectedPayload : EventPayloadBase
    {
        /// <summary>
        /// Gets the grace period for the disconnection in milliseconds.
        /// </summary>
        [DataMember(Name = "gracePeriod")]
        public long GracePeriod { get; set; }

        /// <summary>
        /// Gets the expiration time of the disconnection grace period.
        /// </summary>
        [DataMember(Name = "expiration")]
        public DateTime Expiration { get; set; }

        [DataMember(Name = "playerId")]
        public Guid PlayerId { get; set; }

        public static EventDisconnectedPayload From(PlayerConnection playerConnection)
        {
            return new EventDisconnectedPayload
            {
                PlayerId = playerConnection.Id,
                GracePeriod = playerConnection.DisconnectGracePeriod.Ticks / TimeSpan.TicksPerMillisecond,
                Expiration = DateTime.UtcNow.Add(playerConnection.DisconnectGracePeriod)
            };
        }
    }
}
