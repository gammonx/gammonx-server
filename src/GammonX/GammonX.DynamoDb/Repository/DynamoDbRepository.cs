using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;

using GammonX.DynamoDb.Items;

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
        public async Task<IEnumerable<T>> GetItemsAsync<T>(Guid pkId, string sk)
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
            return response.Items.Select(factory.CreateItem);
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

        // <inheritdoc />
        public async Task<bool> DeleteAsync<T>(Guid pkId)
		{
            var factory = ItemFactoryCreator.Create<T>();
			var pk = string.Format(factory.PKFormat, pkId);
            var deleteKey = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue(pk) },
            };

            var deletePlayerReq = new DeleteItemRequest
            {
                TableName = _tableName,
                Key = deleteKey
            };
            var response = await _client.DeleteItemAsync(deletePlayerReq);
			return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }

		#endregion Generic ItemType
	}
}
