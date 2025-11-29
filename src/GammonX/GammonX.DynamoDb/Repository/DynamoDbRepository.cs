using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;

using GammonX.DynamoDb.Items;
using GammonX.Models.Enums;

using Microsoft.Extensions.Options;

namespace GammonX.DynamoDb.Repository
{
	// <inheritdoc />
	public class DynamoDbRepository : IDynamoDbRepository
	{
		private readonly IAmazonDynamoDB _client;
		private readonly IDynamoDBContext _context;
		private readonly string _tableName = string.Empty;

		public DynamoDbRepository(IAmazonDynamoDB client, IDynamoDBContext context, IOptions<DynamoDbOptions> options)
		{
			_client = client;
			_context = context;
			_tableName = options.Value.DYNAMODB_TABLENAME;
		}

		#region Generic ItemType

		// <inheritdoc />
		public async Task<IEnumerable<T>> GetItems<T>(Guid pkId)
		{
			var factory = ItemFactoryCreator.Create<T>();			
			var pk = string.Format(factory.PKFormat, pkId);
			var sk = factory.SKPrefix;
			var request = new QueryRequest
			{
				TableName = _tableName,
				KeyConditionExpression = "PK = :pk and begins_with(SK, :skPrefix)",
				ExpressionAttributeValues = new Dictionary<string, AttributeValue>
				{
					{ ":pk", new AttributeValue(pk) },
					{ ":skPrefix", new AttributeValue(sk) }
				}
			};
			var response = await _client.QueryAsync(request);
			return response.Items.Select(factory.CreateItem);
		}

		// <inheritdoc />
		public async Task<IEnumerable<T>> GetItemsByGSI<T>(Guid gsi1PkId)
		{
			var factory = ItemFactoryCreator.Create<T>();
			var gsi1pk = string.Format(factory.GSI1PKFormat, gsi1PkId);
			var gsi1sk = factory.GSI1SKPrefix;
			var request = new QueryRequest
			{
				TableName = _tableName,
				IndexName = "GSI1",
				KeyConditionExpression = "GSI1PK = :gsi1pk and begins_with(GSI1SK, :gsi1skPrefix)",
				ExpressionAttributeValues = new Dictionary<string, AttributeValue>
				{
					{ ":gsi1pk", new AttributeValue(gsi1pk) },
					{ ":gsi1skPrefix", new AttributeValue(gsi1sk) }
				}
			};
			var response = await _client.QueryAsync(request);
			return response.Items.Select(factory.CreateItem);
		}

		// <inheritdoc />
		public async Task SaveAsync<T>(T item)
		{
			var factory = ItemFactoryCreator.Create<T>();
			var itemDict = factory.CreateItem(item);
			var request = new PutItemRequest
			{
				TableName = _tableName,
				Item = itemDict
			};
			await _client.PutItemAsync(request);
		}

		#endregion Generic ItemType

		#region Player ItemType

		// TODO: move match to generic

		// <inheritdoc />
		public async Task<PlayerItem?> GetAsync(Guid playerId)
		{
			var pk = string.Format(PlayerItem.PKFormat, playerId);
			var sk = PlayerItem.SKValue;

			var request = new GetItemRequest
			{
				TableName = _tableName,
				Key = new Dictionary<string, AttributeValue>
			{
				{ "PK", new AttributeValue(pk) },
				{ "SK", new AttributeValue(sk) }
			}
			};

			var response = await _client.GetItemAsync(request);
			if (!response.IsItemSet)
				return null;

			return new PlayerItem
			{
				Id = Guid.Parse(response.Item["Id"].S),
				UserName = response.Item["Username"].S,
				CreatedAt = DateTime.Parse(response.Item["CreatedAt"].S)
			};
		}

		// <inheritdoc />
		public async Task SaveAsync(PlayerItem player)
		{
			var pk = string.Format(PlayerItem.PKFormat, player.Id);
			var sk = PlayerItem.SKValue;
			var item = new Dictionary<string, AttributeValue>
			{
				{ "PK", new AttributeValue(pk) },
				{ "SK", new AttributeValue(sk) },
				{ "Id", new AttributeValue(player.Id.ToString()) },
				{ "ItemType", new AttributeValue(ItemTypes.PlayerItemType) },
				{ "Username", new AttributeValue(player.UserName) },
				{ "CreatedAt", new AttributeValue { S = player.CreatedAt.ToString("o") } }
			};

			var request = new PutItemRequest
			{
				TableName = _tableName,
				Item = item
			};
			await _client.PutItemAsync(request);
		}

		// <inheritdoc />
		public async Task DeleteAsync(Guid playerId)
		{
			var pk = string.Format(PlayerItem.PKFormat, playerId);
			var sk = PlayerItem.SKValue;

			// delete player rating items
			var playerRatings = await GetRatingsAsync(playerId);
			foreach (var rating in playerRatings)
			{
				var deleteRatingKey = new Dictionary<string, AttributeValue>
				{
					{ "PK", new AttributeValue(rating.PK) },
					{ "SK", new AttributeValue(rating.SK) }
				};
				var deleteRatingRequest = new DeleteItemRequest
				{
					TableName = _tableName,
					Key = deleteRatingKey
				};
				await _client.DeleteItemAsync(deleteRatingRequest);
			}
			var playerMatches = await GetMatchesOfPlayerAsync(playerId);
			foreach (var match in playerMatches)
			{
				var deleteMatchKey = new Dictionary<string, AttributeValue>
				{
					{ "PK", new AttributeValue(match.PK) },
					{ "SK", new AttributeValue(match.SK) }
				};
				var deleteMatchRequest = new DeleteItemRequest
				{
					TableName = _tableName,
					Key = deleteMatchKey
				};
				await _client.DeleteItemAsync(deleteMatchRequest);
			}

			// delete player item
			var deletePlayerKey = new Dictionary<string, AttributeValue>
			{
				{ "PK", new AttributeValue(pk) },
				{ "SK", new AttributeValue(sk) }
			};

			var deletePlayerReq = new DeleteItemRequest
			{
				TableName = _tableName,
				Key = deletePlayerKey
			};
			await _client.DeleteItemAsync(deletePlayerReq);
		}

		#endregion Player ItemType

		#region PlayerRating ItemType

		// TODO: move match to generic

		// <inheritdoc />
		public async Task<IEnumerable<PlayerRatingItem>> GetRatingsAsync(Guid playerId)
		{
			var pk = string.Format(PlayerRatingItem.PKFormat, playerId.ToString());
			var sk = PlayerRatingItem.SKPrefix;
			var request = new QueryRequest
			{
				TableName = _tableName,
				KeyConditionExpression = "PK = :pk and begins_with(SK, :ratingPrefix)",
				ExpressionAttributeValues = new Dictionary<string, AttributeValue>
				{
					{ ":pk", new AttributeValue(pk) },
					{ ":ratingPrefix", new AttributeValue(sk) }
				}
			};

			var response = await _client.QueryAsync(request);

			return response.Items.Select(item => new PlayerRatingItem
			{
				PlayerId = Guid.Parse(item["Id"].S),
				Variant = Enum.Parse<MatchVariant>(item["Variant"].S, true),
				Type = Enum.Parse<Models.Enums.MatchType>(item["Type"].S, true),
				Modus = Enum.Parse<MatchModus>(item["Modus"].S, true),
				Rating = int.Parse(item["Rating"].N),
				HighestRating = int.Parse(item["HighestRating"].N),
				LowestRating = int.Parse(item["LowestRating"].N)
			});
		}

		// <inheritdoc />
		public async Task SaveAsync(PlayerRatingItem playerRating)
		{
			var variantStr = playerRating.Variant.ToString();
			var modusStr = playerRating.Modus.ToString();
			var typeStr = playerRating.Type.ToString();
			var pk = string.Format(PlayerRatingItem.PKFormat, playerRating.PlayerId);
			var sk = string.Format(PlayerRatingItem.SKFormat, variantStr);
			var item = new Dictionary<string, AttributeValue>
			{
				{ "PK", new AttributeValue(pk) },
				{ "SK", new AttributeValue(sk) },
				{ "Id", new AttributeValue(playerRating.PlayerId.ToString()) },
				{ "ItemType", new AttributeValue(ItemTypes.PlayerRatingItemType) },
				{ "Variant", new AttributeValue(variantStr) },
				{ "Modus", new AttributeValue(modusStr) },
				{ "Type", new AttributeValue(typeStr) },
				{ "Rating", new AttributeValue() { N = playerRating.Rating.ToString() } },
				{ "HighestRating", new AttributeValue() { N = playerRating.HighestRating.ToString() } },
				{ "LowestRating", new AttributeValue() { N = playerRating.LowestRating.ToString() } }
			};

			var request = new PutItemRequest
			{
				TableName = _tableName,
				Item = item
			};
			await _client.PutItemAsync(request);
		}

		#endregion PlayerRating ItemType

		#region Match ItemType

		// TODO: move match to generic

		// <inheritdoc />
		public async Task<IEnumerable<MatchItem>> GetMatchesAsync(Guid matchId)
		{
			var pk = string.Format(MatchItem.PKFormat, matchId);
			var sk = MatchItem.SKPrefix;
			var request = new QueryRequest
			{
				TableName = _tableName,
				KeyConditionExpression = "PK = :pk and begins_with(SK, :skPrefix)",
				ExpressionAttributeValues = new Dictionary<string, AttributeValue>
				{
					{ ":pk", new AttributeValue(pk) },
					{ ":skPrefix", new AttributeValue(sk) }
				}
			};

			var response = await _client.QueryAsync(request);
			return response.Items.Select(CreateMatchItem);
		}

		// <inheritdoc />
		public async Task<IEnumerable<MatchItem>> GetMatchesOfPlayerAsync(Guid playerId)
		{
			var gsi1pk = string.Format(MatchItem.GSI1PKFormat, playerId);
			var gsi1sk = MatchItem.GSI1SKPrefix;
			var request = new QueryRequest
			{
				TableName = _tableName,
				IndexName = "GSI1",
				KeyConditionExpression = "GSI1PK = :gsi1pk and begins_with(GSI1SK, :gsi1skPrefix)",
				ExpressionAttributeValues = new Dictionary<string, AttributeValue>
				{
					{ ":gsi1pk", new AttributeValue(gsi1pk) },
					{ ":gsi1skPrefix", new AttributeValue(gsi1sk) }
				}
			};

			var response = await _client.QueryAsync(request);
			return response.Items.Select(CreateMatchItem);
		}

		// <inheritdoc />
		public async Task SaveAsync(MatchItem match)
		{
			var variantStr = match.Variant.ToString();
			var modusStr = match.Modus.ToString();
			var typeStr = match.Type.ToString();
			var item = new Dictionary<string, AttributeValue>
			{
				{ "PK", new AttributeValue(match.PK) },
				{ "SK", new AttributeValue(match.SK) },
				{ "GSI1PK", new AttributeValue(match.GSI1PK) },
				{ "GSI1SK", new AttributeValue(match.GSI1SK) },
				{ "ItemType", new AttributeValue(ItemTypes.MatchItemType) },
				{ "Id", new AttributeValue(match.Id.ToString()) },
				{ "PlayerId", new AttributeValue(match.PlayerId.ToString()) },
				{ "Points", new AttributeValue() { N = match.Points.ToString() } },
				{ "Length", new AttributeValue() { N = match.Length.ToString() } },
				{ "Variant", new AttributeValue(variantStr) },
				{ "Modus", new AttributeValue(modusStr) },
				{ "Type", new AttributeValue(typeStr) },
				{ "StartedAt", new AttributeValue { S = match.StartedAt.ToString("o") } },
				{ "EndedAt", new AttributeValue { S = match.EndedAt.ToString("o") } },
				{ "Won", new AttributeValue { BOOL = match.Won } }
			};

			var request = new PutItemRequest
			{
				TableName = _tableName,
				Item = item
			};
			await _client.PutItemAsync(request);
		}

		private static MatchItem CreateMatchItem(Dictionary<string, AttributeValue> item)
		{
			var matchItem = new MatchItem
			{
				Id = Guid.Parse(item["Id"].S),
				PlayerId = Guid.Parse(item["PlayerId"].S),
				Points = int.Parse(item["Points"].N),
				Length = int.Parse(item["Length"].N),
				Variant = Enum.Parse<MatchVariant>(item["Variant"].S, true),
				Type = Enum.Parse<Models.Enums.MatchType>(item["Type"].S, true),
				Modus = Enum.Parse<MatchModus>(item["Modus"].S, true),
				StartedAt = DateTime.Parse(item["StartedAt"].S),
				EndedAt = DateTime.Parse(item["EndedAt"].S),
				Won = item["Won"].BOOL ?? false
			};
			matchItem.Won = matchItem.SK.Contains("WON");
			return matchItem;
		}

		#endregion

		#region Game ItemType

		// game item type specific operations

		#endregion Game ItemType
	}
}
