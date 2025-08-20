using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public class PlayerContract
	{
		[DataMember(Name = "id")]
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the match score for this player.
		/// </summary>
		[DataMember(Name = "score")]
		public int Score { get; set; }
	}
}
