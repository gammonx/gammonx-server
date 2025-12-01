using Amazon.DynamoDBv2.DataModel;

using GammonX.Models.Enums;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.DynamoDb.Items
{
	public class PlayerStatsItem
	{
		/// <summary>
		/// Gets a primary key like 'PLAYER#{playerId}'
		/// </summary>
		[DynamoDBHashKey("PK")]
		public string PK => ConstructPK();

		/// <summary>
		/// Gets a sort key like 'STATS#{Variant}#{Type}#{Modus}'
		/// </summary>
		[DynamoDBRangeKey("SK")]
		public string SK => ConstructSK();

		/// <summary>
		/// Gets or sets the id of the <see cref="PlayerItem"/> this stat belongs to."/>
		/// </summary>
		public Guid PlayerId { get; set; } = Guid.Empty;

		public string ItemType { get; } = ItemTypes.PlayerStatsItemType;

		public MatchVariant Variant { get; set; } = MatchVariant.Unknown;

		public MatchModus Modus { get; set; } = MatchModus.Unknown;

		public MatchType Type { get; set; } = MatchType.Unknown;

		public int MatchesPlayed { get; set; } = 0;

		public int MatchesWon { get; set; } = 0;

		public int MatchesLost { get; set; } = 0;

		public double WinRate { get; set; } = 0;

		public int WinStreak { get; set; } = 0;

		public int LongestWinStreak { get; set; } = 0;

		public TimeSpan TotalPlayTime { get; set; } = TimeSpan.Zero;

		public DateTime LastMatch { get; set; } = DateTime.MinValue;

		public int MatchesLast7 { get; set; } = 0;

		public int MatchesLast30 { get; set; } = 0;

		#region Averages

		/// <summary>
		/// Gets or sets the average of <see cref="GameResult.Gammon"/> wins.
		/// </summary>
		public double AvgGammons { get; set; } = 0;

		/// <summary>
		/// Gets or sets the average of <see cref="GameResult.Backgammon"/> wins.
		/// </summary>
		public double AvgBackgammons { get; set; } = 0;

		/// <summary>
		/// Gets the average duration of the matches.
		/// </summary>
		public TimeSpan AvgDuration { get; set; } = TimeSpan.Zero;

		#endregion Averages

		#region Weighted Averages

		/// <summary>
		/// Gets or sets the weighted average pipes left for the games which were lost.
		/// </summary>
		public double WAvgPipesLeft { get; set; } = 0;

		/// <summary>
		/// Gets or sets the weighted average amount of double dices rolled per game
		/// </summary>
		public double WAvgDoubleDices { get; set; } = 0;

		/// <summary>
		/// Gets or sets the weighted average turns per game.
		/// </summary>
		public double WAvgTurns { get; set; } = 0;

		/// <summary>
		/// Gets or sets the weighted average amount of doubling cube offers.
		/// </summary>
		public double WAvgDoubles { get; set; } = 0;

		/// <summary>
		/// Gets or sets the wieghted average duration per game.
		/// </summary>
		public TimeSpan WAvgDuration { get; set; } = TimeSpan.Zero;

		#endregion Weighted Averages

		private string ConstructPK()
		{
			var factory = ItemFactoryCreator.Create<PlayerStatsItem>();
            return string.Format(factory.PKFormat, PlayerId);
		}

		private string ConstructSK()
		{
			var factory = ItemFactoryCreator.Create<PlayerStatsItem>();
            return string.Format(factory.SKFormat, Variant, Type, Modus);
		}
	}
}
