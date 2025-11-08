using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Serilog;

namespace GammonX.Server.Data.DynamoDb
{
	public static class DynamoDbInitializer
	{
		public static async Task EnsureTablesExistAsync(IAmazonDynamoDB client)
		{
			const string tableName = Constants.TableName;

			var existingTables = await client.ListTablesAsync();
			if (existingTables.TableNames.Contains(tableName))
			{
				Log.Information("DYNAMO DB: {TableName} found", tableName);
				return;
			}

			var request = new CreateTableRequest
			{
				TableName = tableName,
				AttributeDefinitions = new List<AttributeDefinition>
				{
					new("PK", ScalarAttributeType.S),
					new("SK", ScalarAttributeType.S)
				},
				KeySchema = new List<KeySchemaElement>
				{
					new("PK", KeyType.HASH),
					new("SK", KeyType.RANGE)
				},
				BillingMode = BillingMode.PAY_PER_REQUEST,
			};

			await client.CreateTableAsync(request);
			Log.Information("DYNAMO DB: {TableName} created", tableName);
		}
	}
}
