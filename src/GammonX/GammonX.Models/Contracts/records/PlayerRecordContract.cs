using System.Runtime.Serialization;

namespace GammonX.Models.Contracts
{
    /// <summary>
	/// Contract providing a created player to the simple queue service.
	/// </summary>
    [DataContract]
    public class PlayerRecordContract
    {
        /// <summary>
        /// Gets or sets the player id.
        /// </summary>
        /// <remarks>
        /// The player id is a central aspect of the whole backend service. It uniquly identifies records in the database
        /// relating to a single player, as well as uniquly identifying a player during a game within the engine.
        /// </remarks>
        [DataMember(Name = "Id")]
        public Guid Id { get; set; } = Guid.Empty;

        /// <summary>
        /// Gets or sets the user name of the given player.
        /// </summary>
        [DataMember(Name = "Username")]
        public string UserName { get; set; } = string.Empty;
    }
}
