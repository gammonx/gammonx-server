using Amazon.DynamoDBv2;

using DotNetEnv;

using GammonX.DynamoDb;
using GammonX.DynamoDb.Extensions;
using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;
using GammonX.DynamoDb.Services;

using GammonX.Lambda.Handlers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GammonX.Lambda
{
	public static class Startup
	{
		private static IServiceProvider? _provider;

		public static async Task ConfigureDynamoDbTableAsync(IServiceProvider services)
		{
            using (var scope = services.CreateScope())
            {
                var options = services.GetRequiredService<IOptions<DynamoDbOptions>>().Value;
                if (options.Required)
                {
                    var dynamoClient = scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>();
                    await DynamoDbInitializer.EnsureTablesExistAsync(dynamoClient, options);
                }
            }
        }

		public static IServiceProvider Configure()
		{
			if (_provider != null)
				return _provider;

			var services = new ServiceCollection();

            // -------------------------------------------------------------------------------
            // ENVIRONMENT SETUP
            // -------------------------------------------------------------------------------
            var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
			// we only want to load the .env files if ran outside of docker.
			if (!isDocker)
			{
                var envLocal = Path.Combine(Directory.GetCurrentDirectory(), ".env.local");
                var env = Path.Combine(Directory.GetCurrentDirectory(), ".env");
                if (File.Exists(envLocal))
                {
                    Env.Load(envLocal);
                }
                else if (File.Exists(env))
                {
                    Env.Load(env);
                }
            }
			// -------------------------------------------------------------------------------
			// CONFIGURATION SETUP
			// -------------------------------------------------------------------------------
			var configuration = new ConfigurationBuilder()
				.AddEnvironmentVariables()
				.Build();
			services.Configure<DynamoDbOptions>(configuration.GetSection("AWS"));
			// -------------------------------------------------------------------------------
			// SERVICES SETUP
			// -------------------------------------------------------------------------------
			services.AddSingleton<IDynamoDbRepository, DynamoDbRepository>();
			services.AddKeyedTransient<ISqsLambdaHandler, MatchCompletedHandler>(LambdaFunctions.MatchCompletedFunc);
			services.AddKeyedTransient<ISqsLambdaHandler, GameCompletedHandler>(LambdaFunctions.GameCompletedFunc);
			services.AddKeyedTransient<ISqsLambdaHandler, PlayerRatingUpdatedHandler>(LambdaFunctions.PlayerRatingUpdatedFunc);
			services.AddKeyedTransient<ISqsLambdaHandler, PlayerStatsUpdatedHandler>(LambdaFunctions.PlayerStatsUpdatedFunc);
			services.AddKeyedTransient<ISqsLambdaHandler, PlayerCreatedHandler>(LambdaFunctions.PlayerCreatedFunc);

			services.AddKeyedTransient<IApiLambdaHandler, GetPlayerRatingHandler>(typeof(GetPlayerRatingHandler));
			// -------------------------------------------------------------------------------
			// DATABASE SETUP
			// -------------------------------------------------------------------------------
			var awsConfig = configuration.GetSection("AWS");
			services.AddConditionalDynamoDb(awsConfig);

			_provider = services.BuildServiceProvider();

			return _provider;
		}
	}
}
