using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Serilog;

namespace GammonX.Server.Data.DynamoDb
{
	public static class DynamoDbInitializer
	{
		public static async Task EnsureTablesExistAsync(IAmazonDynamoDB client, AwsServiceOptions options)
		{
			var tableName = options.DYNAMODB_TABLENAME;

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
					new("SK", ScalarAttributeType.S),
					new("GSI1PK", ScalarAttributeType.S),
					new("GSI1SK", ScalarAttributeType.S),
				},
				KeySchema = new List<KeySchemaElement>
				{
					new("PK", KeyType.HASH),
					new("SK", KeyType.RANGE)
				},
				GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
				{
					new GlobalSecondaryIndex
					{
						IndexName = "GSI1",
						KeySchema = new List<KeySchemaElement>
						{
							new("GSI1PK", KeyType.HASH),
							new("GSI1SK", KeyType.RANGE)
						},
						Projection = new Projection
						{
							ProjectionType = ProjectionType.ALL
						},
						// only for provisioned billing mode
						//ProvisionedThroughput = new ProvisionedThroughput
						//{
						//	ReadCapacityUnits = 5,
						//	WriteCapacityUnits = 5
						//}
					}
				},
				BillingMode = BillingMode.PAY_PER_REQUEST,
			};

			await client.CreateTableAsync(request);
			Log.Information("DYNAMO DB: {TableName} created", tableName);
		}
	}
}
