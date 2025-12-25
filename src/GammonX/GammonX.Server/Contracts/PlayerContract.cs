using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
    [DataContract]
    public class PlayerContract
    {
        /// <summary>
        /// Gets or sets the player id.
        /// </summary>
        [DataMember(Name = "id", IsRequired = true, EmitDefaultValue = false)]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the player username.
        /// </summary>
        [DataMember(Name = "userName", IsRequired = false, EmitDefaultValue = true)]
        public string? UserName { get; set; }

        /// <summary>
        /// Gets or sets the match score for this player.
        /// </summary>
        [DataMember(Name = "points", IsRequired = false, EmitDefaultValue = true)]
        public int? Points { get; set; }

        /// <summary>
        /// Gets or sets the starting dice roll of the player.
        /// </summary>
        [DataMember(Name = "startDiceRoll", IsRequired = false, EmitDefaultValue = true)]
        public int? StartDiceRoll { get; set; }
    }
}
