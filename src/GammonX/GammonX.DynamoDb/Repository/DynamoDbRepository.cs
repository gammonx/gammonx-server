using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;

using GammonX.DynamoDb.Items;

using Microsoft.Extensions.Options;

namespace GammonX.DynamoDb.Repository
{
	// <inheritdoc />
	public class DynamoDbRepository : IDynamoDbRepository, IDynamoDbBatchWriter, IDynamoDbTransactionWriter, IDynamoDbConsistentReader
	{
		private const int MaxBatchWriteAttempts = 5;

		private readonly IAmazonDynamoDB _client;

		private readonly string _tableName;

		public DynamoDbRepository(IAmazonDynamoDB client, IDynamoDBContext context, IOptions<DynamoDbOptions> options)
		{
			_client = client;
			_ = context;
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
			return await QueryAllAsync(request, factory);
		}

		// <inheritdoc />
		public async Task<IEnumerable<T>> GetItemsAsync<T>(Guid pkId, string sk)
		{
			return await GetItemsAsync<T>(pkId, sk, false);
		}

		// <inheritdoc />
		public async Task<IEnumerable<T>> GetItemsConsistentlyAsync<T>(Guid pkId, string sk)
		{
			return await GetItemsAsync<T>(pkId, sk, true);
		}

		private async Task<IEnumerable<T>> GetItemsAsync<T>(Guid pkId, string sk, bool consistentRead)
		{
			var factory = ItemFactoryCreator.Create<T>();
			var pk = string.Format(factory.PKFormat, pkId);
			var request = new QueryRequest
			{
				TableName = _tableName,
				ConsistentRead = consistentRead,
				KeyConditionExpression = "PK = :pk and begins_with(SK, :skPrefix)",
				ExpressionAttributeValues = new Dictionary<string, AttributeValue>
				{
					{ ":pk", new AttributeValue(pk) },
					{ ":skPrefix", new AttributeValue(sk) }
				}
			};
			return await QueryAllAsync(request, factory);
		}

		// <inheritdoc />
		public async Task<IEnumerable<T>> GetItemsByGSIPKAsync<T>(Guid gsi1PkId)
		{
			var factory = ItemFactoryCreator.Create<T>();
			var gsi1PK = string.Format(factory.GSI1PKFormat, gsi1PkId);
			var gsi1SK = factory.GSI1SKPrefix;
			var request = new QueryRequest
			{
				TableName = _tableName,
				IndexName = "GSI1",
				KeyConditionExpression = "GSI1PK = :gsi1pk and begins_with(GSI1SK, :gsi1skPrefix)",
				ExpressionAttributeValues = new Dictionary<string, AttributeValue>
				{
					{ ":gsi1pk", new AttributeValue(gsi1PK) },
					{ ":gsi1skPrefix", new AttributeValue(gsi1SK) }
				}
			};
			return await QueryAllAsync(request, factory);
		}

		// <inheritdoc />
		public async Task<IEnumerable<T>> GetItemsByGSIPKAsync<T>(Guid gsi1PkId, string gsi1Sk)
		{
			var factory = ItemFactoryCreator.Create<T>();
			var gsi1PK = string.Format(factory.GSI1PKFormat, gsi1PkId);
			var request = new QueryRequest
			{
				TableName = _tableName,
				IndexName = "GSI1",
				KeyConditionExpression = "GSI1PK = :gsi1pk and begins_with(GSI1SK, :gsi1sk)",
				ExpressionAttributeValues = new Dictionary<string, AttributeValue>
				{
					{ ":gsi1pk", new AttributeValue(gsi1PK) },
					{ ":gsi1sk", new AttributeValue(gsi1Sk) }
				}
			};
			return await QueryAllAsync(request, factory);
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
		public async Task<bool> DeleteAsync<T>(Guid pkId, string sk)
		{
			var factory = ItemFactoryCreator.Create<T>();
			var deletePlayerReq = new DeleteItemRequest
			{
				TableName = _tableName,
				Key = CreateKey(factory, pkId, sk)
			};
			var response = await _client.DeleteItemAsync(deletePlayerReq);
			return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
		}

		// <inheritdoc />
		public async Task BatchDeleteAsync<T>(IEnumerable<(Guid PkId, string Sk)> keys)
		{
			var factory = ItemFactoryCreator.Create<T>();
			var writeRequests = keys.Select(key => new WriteRequest
			{
				DeleteRequest = new DeleteRequest
				{
					Key = CreateKey(factory, key.PkId, key.Sk)
				}
			});

			foreach (var chunk in writeRequests.Chunk(25))
			{
				var pending = chunk.ToList();

				for (var attempt = 1; pending.Count > 0; attempt++)
				{
					if (attempt > MaxBatchWriteAttempts)
						throw new InvalidOperationException($"Failed to delete {pending.Count} '{typeof(T).Name}' items after {MaxBatchWriteAttempts} attempts.");

					var response = await _client.BatchWriteItemAsync(new BatchWriteItemRequest
					{
						RequestItems = new Dictionary<string, List<WriteRequest>>
						{
							{ _tableName, pending }
						}
					});
                    
					pending = response.UnprocessedItems.TryGetValue(_tableName, out var unprocessed)
						? unprocessed
						: [];

					if (pending.Count > 0)
					{
						var backoffMs = (1 << (attempt - 1)) * 25 + Random.Shared.Next(0, 25);
						await Task.Delay(backoffMs);
					}
				}
			}
		}

		// <inheritdoc />
		public async Task TransactPutAsync(IEnumerable<DynamoDbPutOperation> operations)
		{
			var transactionItems = operations.Select(operation =>
			{
				var put = new Put
				{
					TableName = _tableName,
					Item = operation.Item,
					ConditionExpression = operation.ConditionExpression
				};

				if (operation.ExpressionAttributeNames.Count > 0)
				{
                    put.ExpressionAttributeNames = new Dictionary<string, string>(operation.ExpressionAttributeNames);
				}

				if (operation.ExpressionAttributeValues.Count > 0)
				{
                    put.ExpressionAttributeValues = new Dictionary<string, AttributeValue>(operation.ExpressionAttributeValues);
				}

				return new TransactWriteItem { Put = put };
			}).ToList();

			if (transactionItems.Count is 0 or > 100)
			{
                throw new ArgumentOutOfRangeException(nameof(operations), "A DynamoDB transaction must contain between 1 and 100 operations.");
			}

			await _client.TransactWriteItemsAsync(new TransactWriteItemsRequest
			{
				TransactItems = transactionItems
			});
		}

		private async Task<IEnumerable<T>> QueryAllAsync<T>(QueryRequest request, IItemFactory<T> factory)
		{
			var items = new List<T>();

			do
			{
				var response = await _client.QueryAsync(request);
				items.AddRange(response.Items.Select(factory.CreateItem));
				request.ExclusiveStartKey = response.LastEvaluatedKey;
			} while (request.ExclusiveStartKey?.Count > 0);

			return items;
		}

		private static Dictionary<string, AttributeValue> CreateKey<T>(IItemFactory<T> factory, Guid pkId, string sk)
		{
			return new Dictionary<string, AttributeValue>
			{
				{ "PK", new AttributeValue(string.Format(factory.PKFormat, pkId)) },
				{ "SK", new AttributeValue(sk) }
			};
		}

		#endregion Generic ItemType
	}
}
