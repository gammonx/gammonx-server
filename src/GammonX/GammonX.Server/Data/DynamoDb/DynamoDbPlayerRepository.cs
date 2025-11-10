using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;

using GammonX.Server.Data.Entities;
using GammonX.Server.Data.Repository;
using GammonX.Server.Models;
using Microsoft.Extensions.Options;

namespace GammonX.Server.Data.DynamoDb
{
	// <inheritdoc />
	public class DynamoDbPlayerRepository : IPlayerRepository
	{
		private readonly IAmazonDynamoDB _client;
		private readonly IDynamoDBContext _context;
		private readonly string _tableName = string.Empty;

		public DynamoDbPlayerRepository(IAmazonDynamoDB client, IDynamoDBContext context, IOptions<AwsServiceOptions> options)
		{
			_client = client;
			_context = context;
			_tableName = options.Value.DYNAMODB_TABLENAME;
		}

		#region Player ItemType

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
				PK = response.Item["PK"].S,
				SK = response.Item["SK"].S,
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
				{ "Id", new AttributeValue(player.Id.ToString("N")) },
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

		// <inheritdoc />
		public async Task<IEnumerable<PlayerRatingItem>> GetRatingsAsync(Guid playerId)
		{
			var pk = string.Format(PlayerRatingItem.PKFormat, playerId);
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
				PK = item["PK"].S,
				SK = item["SK"].S,
				PlayerId = Guid.Parse(item["Id"].S),
				Variant = Enum.Parse<WellKnownMatchVariant>(item["Variant"].S, true),
				Type = Enum.Parse<WellKnownMatchType>(item["Type"].S, true),
				Modus = Enum.Parse<WellKnownMatchModus>(item["Modus"].S, true),
				Rating = int.Parse(item["Rating"].N),
				HighestRating = int.Parse(item["HighestRating"].N),
				LowestRating = int.Parse(item["LowestRating"].N)
			});
		}

		// <inheritdoc />
		public async Task SaveAsync(PlayerRatingItem playerRating)
		{
			var variantStr = playerRating.Variant.ToString().ToUpper();
			var modusStr = playerRating.Modus.ToString().ToUpper();
			var typeStr = playerRating.Type.ToString().ToUpper();
			var pk = string.Format(PlayerRatingItem.PKFormat, playerRating.PlayerId);
			var sk = string.Format(PlayerRatingItem.SKFormat, variantStr, modusStr);
			var item = new Dictionary<string, AttributeValue>
			{
				{ "PK", new AttributeValue(pk) },
				{ "SK", new AttributeValue(sk) },
				{ "Id", new AttributeValue(playerRating.PlayerId.ToString("N")) },
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
	}
}
