using GammonX.Server.Repository;

using Microsoft.Extensions.Options;

using System.Net.Http.Headers;

namespace GammonX.Server.Extensions
{
    public static class RepositoryServiceExtensions
    {
        public static void AddRepositoryServices(this IServiceCollection services, IConfiguration repositoryOptions)
        {
            services.Configure<RepositoryOptions>(repositoryOptions);
            // we check manually if a real repo client config is required
            var baseUrl = Environment.GetEnvironmentVariable("REPOSITORY__BASEURL");
            if (string.IsNullOrEmpty(baseUrl))
            {
                // we setup a dummy repo client
                services.AddSingleton<IRepositoryClient, SimpleRepositoryClient>();
                Serilog.Log.Information($"RepositoryClient: '{nameof(SimpleRepositoryClient)}'");
                return;
            }

            Serilog.Log.Information($"RepositoryClient: '{nameof(ApiGatewayClient)}' BaseUrl '{baseUrl}'");

            services.AddHttpClient<IRepositoryClient, ApiGatewayClient>((sp, client) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var options = sp.GetRequiredService<IOptions<RepositoryOptions>>().Value;

                client.BaseAddress = new Uri(options.BASEURL);

                client.Timeout = TimeSpan.FromSeconds(20);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }).SetHandlerLifetime(TimeSpan.FromMinutes(5));
        }
    }
}
