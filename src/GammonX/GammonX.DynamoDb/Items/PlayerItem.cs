using Amazon.DynamoDBv2.DataModel;

namespace GammonX.DynamoDb.Items
{
	public class PlayerItem
	{
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
			var factory = ItemFactoryCreator.Create<PlayerItem>();
			return string.Format(factory.PKFormat, Id);
		}

		private static string ConstructSK()
		{
            var factory = ItemFactoryCreator.Create<PlayerItem>();
            return factory.SKPrefix;
		}
	}
}
