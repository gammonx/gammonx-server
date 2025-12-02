using Amazon.DynamoDBv2.Model;

namespace GammonX.DynamoDb.Items
{
    /// <summary>
    /// Provides the capability to create items of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    public interface IItemFactory<T>
	{
		/// <summary>
		/// Gets the format of the primary key (hash key).
		/// </summary>
		string PKFormat { get; }

		/// <summary>
		/// Gets the format of the secondary key (range key).
		/// </summary>
		string SKFormat { get; }

		/// <summary>
		/// Gets the prefix of the secondary key (range key).
		/// </summary>
		string SKPrefix { get; }

		/// <summary>
		/// Gets the global secondary index primary key format.
		/// </summary>
		string GSI1PKFormat { get; }

		/// <summary>
		/// Gets the global secondary index secondary key format.
		/// </summary>
		string GSI1SKFormat { get; }

		/// <summary>
		/// Gets the global secondary index secondary key prefix.
		/// </summary>
		string GSI1SKPrefix { get; }

		/// <summary>
		/// Create an item of type <typeparamref name="T"/>.
		/// </summary>
		/// <param name="item">Item attributes.</param>
		/// <returns>An item instance of <typeparamref name="T"/>.</returns>
		T CreateItem(Dictionary<string, AttributeValue> item);

		/// <summary>
		/// Creates a keyed attribute value list by the given instance of <typeparamref name="T"/>.
		/// </summary>
		/// <param name="item">Type to create from.</param>
		/// <returns>A keyed attribute value dict.</returns>
		Dictionary<string, AttributeValue> CreateItem(T item);
	}

	public static class ItemFactoryCreator
	{
		/// <summary>
		/// Creates an item factory.
		/// </summary>
		/// <typeparam name="T">Type of the related item type.</typeparam>
		/// <returns>A factory creating <typeparamref name="T2"/>.</returns>
		public static IItemFactory<T> Create<T>()
		{
			if (typeof(T) == typeof(GameItem))
				return (IItemFactory<T>)new GameItemFactory();
			if (typeof(T) == typeof(GameHistoryItem))
				return (IItemFactory<T>)new GameHistoryItemFactory();
			if (typeof(T) == typeof(MatchItem))
				return (IItemFactory<T>)new MatchItemFactory();
			if (typeof(T) == typeof(MatchHistoryItem))
				return (IItemFactory<T>)new MatchHistoryItemFactory();
			if (typeof(T) == typeof(PlayerStatsItem))
				return (IItemFactory<T>)new PlayerStatsItemFactory();
            if (typeof(T) == typeof(PlayerRatingItem))
                return (IItemFactory<T>)new PlayerRatingItemFactory();
			if (typeof(T) == typeof(PlayerItem))
				return (IItemFactory<T>) new PlayerItemFactory();
            if (typeof(T) == typeof(RatingPeriodItem))
                return (IItemFactory<T>)new RatingPeriodItemFactory();

            throw new InvalidOperationException($"Unknown item type '{typeof(T)}'");
		}
	}
}
