using GammonX.Server.EntityFramework.Entities;
using GammonX.Server.Models;

using System.Runtime.Serialization;

namespace GammonX.Server.Contracts
{
	[DataContract]
	public class PlayerContract
	{
		[DataMember(Name = "id")]
		public Guid Id { get; set; }

		[DataMember(Name = "userName", IsRequired = false, EmitDefaultValue = false)]
		public string? UserName { get; set; }

		/// <summary>
		/// Gets or sets the match score for this player.
		/// </summary>
		[DataMember(Name = "score", IsRequired = false, EmitDefaultValue = false)]
		public int? Score { get; set; }
	}

	public static class PlayerExtensions
	{
		public static PlayerContract ToContract(this Player player)
		{
			return new PlayerContract
			{
				Id = player.Id,
				UserName = player.UserName,
				Score = null
			};
		}

		public static PlayerContract ToContract(this MatchPlayerModel model)
		{
			return new PlayerContract()
			{
				Id = model.Id,
				Score = model.Points,
				UserName = null
			};
		}
	}
}
