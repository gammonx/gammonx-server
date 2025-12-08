using GammonX.Models.Contracts;
using GammonX.Models.Enums;
using Newtonsoft.Json;

namespace GammonX.Server.Repository
{
    // <inheritdoc />
    public class ApiGatewayClient : IRepositoryClient
    {
        private readonly HttpClient _client;

        // <inheritdoc />
        public string BaseUrl => _client?.BaseAddress?.ToString() ?? string.Empty;

        public ApiGatewayClient(HttpClient client)
        {
            _client = client;
        }

        // <inheritdoc />
        public async Task<PlayerRatingResponseContract?> GetRatingAsync(Guid playerId, MatchVariant variant, CancellationToken cancellationToken)
        {
            try
            {
                var url = $"players/{playerId}/rating/{variant}";
                using var response = await _client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var rating = JsonConvert.DeserializeObject<PlayerRatingResponseContract>(responseJson);
                return rating;
            }
            catch (Exception ex) 
            {
                Serilog.Log.Error(ex, $"An error occurred while requesting rating for '{playerId}' and variant '{variant}'");
                return null;
            } 
        }
    }
}
