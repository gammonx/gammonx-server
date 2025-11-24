using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;

using GammonX.DynamoDb.Repository;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GammonX.DynamoDb.Extensions
{
	public static class DynamoDbServiceExtensions
	{
		public static void AddConditionalDynamoDb(this IServiceCollection services, IConfiguration dynamoDbOptions)
		{
			services.Configure<DynamoDbOptions>(dynamoDbOptions);
			// we check manually if a dynamo config is required
			var tableName = Environment.GetEnvironmentVariable("AWS__DYNAMODB_TABLENAME");
			if (string.IsNullOrEmpty(tableName))
			{
				return;
			}

			services.AddSingleton<IAmazonDynamoDB>(sp =>
			{
				var options = sp.GetRequiredService<IOptions<DynamoDbOptions>>().Value;
				var isLocal = string.IsNullOrEmpty(options.REGION);
				var keyAuth = !string.IsNullOrEmpty(options.AWS_ACCESS_KEY_ID) && !string.IsNullOrEmpty(options.AWS_SECRET_ACCESS_KEY);
				if (isLocal)
				{
					// local docker instance
					var accessKeyId = options.AWS_ACCESS_KEY_ID;
					var secretAccessKey = options.AWS_SECRET_ACCESS_KEY;
					var config = new AmazonDynamoDBConfig
					{
						ServiceURL = options.DYNAMODB_SERVICEURL
					};
					var credentials = new BasicAWSCredentials(options.AWS_ACCESS_KEY_ID, options.AWS_SECRET_ACCESS_KEY);
					return new AmazonDynamoDBClient(credentials, config);
				}
				else if (keyAuth)
				{
					// AWS hosted accessed locally
					var region = RegionEndpoint.GetBySystemName(options.REGION);
					var config = new AmazonDynamoDBConfig
					{
						// We do not need a specific service url, the region endpoint is sufficient
						RegionEndpoint = region
					};
					// We use access key based auth
					var credentials = new BasicAWSCredentials(options.AWS_ACCESS_KEY_ID, options.AWS_SECRET_ACCESS_KEY);
					return new AmazonDynamoDBClient(credentials, config);
				}
				else
				{
					// AWS hosted
					var region = RegionEndpoint.GetBySystemName(options.REGION);
					var config = new AmazonDynamoDBConfig
					{
						// We do not need a specific service url, the region endpoint is sufficient
						RegionEndpoint = region
					};
					// We use role based auth
					return new AmazonDynamoDBClient(config);
				}
			});
			services.AddSingleton<IDynamoDBContext>(sp =>
			{
				var client = sp.GetRequiredService<IAmazonDynamoDB>();
				var contextBuilder = new DynamoDBContextBuilder();
				contextBuilder.WithDynamoDBClient(() => client);
				return contextBuilder.Build();
			});

			services.AddScoped<IDynamoDbRepository, DynamoDbRepository>();
		}
	}
}
