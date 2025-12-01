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
		public async Task<IEnumerable<T>> GetItemsAsync<T>(Guid pkId)
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
        public async Task<T?> GetItemAsync<T>(Guid pkId, string sk)
        {
            var factory = ItemFactoryCreator.Create<T>();
            var pk = string.Format(factory.PKFormat, pkId);
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
			var result = response.Items.FirstOrDefault();
			if (result != null)
			{
                return factory.CreateItem(result);
            }
            return default;
        }

        // <inheritdoc />
        public async Task<IEnumerable<T>> GetItemsByGSIPKAsync<T>(Guid gsi1PkId)
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
		public async Task<IEnumerable<T>> GetItemsByGSIPKAsync<T>(Guid gsi1PkId, string gsi1Sk)
		{
			var factory = ItemFactoryCreator.Create<T>();
			var gsi1pk = string.Format(factory.GSI1PKFormat, gsi1PkId);
			var request = new QueryRequest
			{
				TableName = _tableName,
				IndexName = "GSI1",
				KeyConditionExpression = "GSI1PK = :gsi1pk and begins_with(GSI1SK, :gsi1sk)",
				ExpressionAttributeValues = new Dictionary<string, AttributeValue>
				{
					{ ":gsi1pk", new AttributeValue(gsi1pk) },
					{ ":gsi1sk", new AttributeValue(gsi1Sk) }
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

		// TODO: move player to generic

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
			var playerRatings = await GetItemsAsync<PlayerRatingItem>(playerId);
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
			var playerMatches = await GetItemsByGSIPKAsync<MatchItem>(playerId);
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
	}
}
