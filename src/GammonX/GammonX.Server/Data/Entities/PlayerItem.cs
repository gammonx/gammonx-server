using Amazon.DynamoDBv2.DataModel;
using GammonX.Server.Data.DynamoDb;

namespace GammonX.Server.Data.Entities
{
	public class PlayerItem
	{
		public const string PKFormat = "PLAYER#{0}";

		public const string SKValue = "PROFILE";

		/// <summary>
		/// Gets a primary key like 'PLAYER#{playerId}'
		/// </summary>
		[DynamoDBHashKey("PK")]
		public string PK { get; set; } = string.Empty;

		/// <summary>
		/// Gets a sort key like 'PROFILE'
		/// </summary>
		[DynamoDBRangeKey("SK")]
		public string SK { get; set; } = string.Empty;

		public Guid Id { get; set; } = Guid.Empty;

		public string ItemType { get; } = ItemTypes.PlayerItemType;

		public string UserName { get; set; } = string.Empty;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}
