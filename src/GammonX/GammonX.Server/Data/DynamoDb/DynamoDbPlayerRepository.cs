using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;

using GammonX.Server.Data.Entities;
using GammonX.Server.Data.Repository;
using GammonX.Server.Models;

namespace GammonX.Server.Data.DynamoDb
{
	// <inheritdoc />
	public class DynamoDbPlayerRepository : IPlayerRepository
	{
		// TODO: implement generic base DynamoDbRepositoryBase<T>

		private readonly IAmazonDynamoDB _client;
		private readonly IDynamoDBContext _context;
		private readonly string _tableName = Constants.TableName;

		public DynamoDbPlayerRepository(IAmazonDynamoDB client, IDynamoDBContext context)
		{
			_client = client;
			_context = context;
		}

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

			// TODO: serialization with DynamoDBContext instead of dictionary mapping?
			return new PlayerItem
			{
				Id = playerId,
				UserName = response.Item["Username"].S,
				CreatedAt = DateTime.Parse(response.Item["CreatedAt"].S),
				PK = pk,
				SK = sk
			};
		}

		// <inheritdoc />
		public async Task<IEnumerable<PlayerRatingItem>> GetRatingsAsync(Guid playerId, WellKnownMatchVariant variant, WellKnownMatchModus modus)
		{
			var pk = string.Format(PlayerItem.PKFormat, playerId);
			var sk = string.Format(PlayerRatingItem.SKFormat, variant.ToString().ToUpper(), modus.ToString().ToUpper());
			var request = new QueryRequest
			{
				TableName = _tableName,
				KeyConditionExpression = "PK = :pk and SK = :sk",
				ExpressionAttributeValues = new Dictionary<string, AttributeValue>
			{
				{ ":pk", new AttributeValue(pk) },
				{ ":sk", new AttributeValue(sk) }
			}
			};

			var response = await _client.QueryAsync(request);

			return response.Items.Select(item => new PlayerRatingItem
			{
				Id = playerId,
				Variant = Enum.Parse<WellKnownMatchVariant>(item["Variant"].S),
				Type = Enum.Parse<WellKnownMatchType>(item["Type"].S),
				Modus = Enum.Parse<WellKnownMatchModus>(item["Modus"].S),
				Rating = int.Parse(item["Rating"].N),
				HighestRating = int.Parse(item["HighestRating"].N),
				LowestRating = int.Parse(item["LowestRating"].N)
			});
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
	}
}
