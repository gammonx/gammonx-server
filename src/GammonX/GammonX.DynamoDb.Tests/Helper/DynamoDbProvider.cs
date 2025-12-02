using DotNetEnv;

using GammonX.DynamoDb.Extensions;
using GammonX.DynamoDb.Repository;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GammonX.DynamoDb.Tests.Helper
{
    public static class DynamoDbProvider
    {
        private static IServiceProvider? _provider;

        public static IServiceProvider Configure()
        {
            if (_provider != null)
                return _provider;

            var services = new ServiceCollection();

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

            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
            services.Configure<DynamoDbOptions>(configuration.GetSection("AWS"));

            services.AddSingleton<IDynamoDbRepository, DynamoDbRepository>();

            var awsConfig = configuration.GetSection("AWS");
            services.AddConditionalDynamoDb(awsConfig);

            _provider = services.BuildServiceProvider();

            return _provider;
        }
    }
}
