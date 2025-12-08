using Amazon.DynamoDBv2.DataModel;

using GammonX.Models.Enums;

namespace GammonX.DynamoDb.Items
{
	public class GameHistoryItem
	{
		/// <summary>
		/// Gets a primary key like 'GAME#{gameId}'
		/// </summary>
		[DynamoDBHashKey("PK")]
		public string PK => ConstructPK();

		/// <summary>
		/// Gets a sort key like 'HISTORY'
		/// </summary>
		[DynamoDBRangeKey("SK")]
		public string SK => ConstructSK();

		/// <summary>
		/// Gets or sets the game id.
		/// </summary>
		public Guid GameId { get; set; } = Guid.Empty;

		public string ItemType { get; } = ItemTypes.GameHistoryItemType;

		/// <summary>
		/// Gets or sets the history in a string format.
		/// </summary>
		public string Data { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the format of the given <see cref="Data"/> string.
		/// </summary>
		public HistoryFormat Format { get; set; } = HistoryFormat.Unknown;

		private string ConstructPK()
		{
			var factory = ItemFactoryCreator.Create<GameHistoryItem>();
			return string.Format(factory.PKFormat, GameId);
		}

		private static string ConstructSK()
		{
			var factory = ItemFactoryCreator.Create<GameHistoryItem>();
            return factory.SKFormat;
		}
	}
}
