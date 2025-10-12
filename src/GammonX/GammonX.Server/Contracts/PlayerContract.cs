using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public class PlayerContract
	{
		/// <summary>
		/// Gets or sets the player id.
		/// </summary>
		[DataMember(Name = "id")]
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the player username.
		/// </summary>
		[DataMember(Name = "userName", IsRequired = false, EmitDefaultValue = false)]
		public string? UserName { get; set; }

		/// <summary>
		/// Gets or sets the match score for this player.
		/// </summary>
		[DataMember(Name = "score", IsRequired = false, EmitDefaultValue = false)]
		public int? Points { get; set; }
	}
}
