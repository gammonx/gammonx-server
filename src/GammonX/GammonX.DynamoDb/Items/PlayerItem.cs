using Amazon.DynamoDBv2.DataModel;

namespace GammonX.DynamoDb.Items
{
	public class PlayerItem
	{
		public const string PKFormat = "PLAYER#{0}";

		public const string SKValue = "PROFILE";

		/// <summary>
		/// Gets a primary key like 'PLAYER#{playerId}'
		/// </summary>
		[DynamoDBHashKey("PK")]
		public string PK => ConstructPK();

		/// <summary>
		/// Gets a sort key like 'PROFILE'
		/// </summary>
		[DynamoDBRangeKey("SK")]
		public string SK => ConstructSK();

		public Guid Id { get; set; } = Guid.Empty;

		public string ItemType { get; } = ItemTypes.PlayerItemType;

		public string UserName { get; set; } = string.Empty;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		private string ConstructPK()
		{
			return string.Format(PKFormat, Id);
		}

		private string ConstructSK()
		{
			return SKValue;
		}
	}
}
