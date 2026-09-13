using GammonX.Models.Contracts;
using GammonX.Models.Enums;
using GammonX.Models.Helpers;

using System.Text.Json;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Repository
{
    // <inheritdoc />
    public class ApiGatewayClient : IRepositoryClient
    {
        private readonly HttpClient _client;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new UtcDateTimeJsonConverter() }
        };

        // <inheritdoc />
        public string BaseUrl => _client.BaseAddress?.ToString() ?? string.Empty;

        public ApiGatewayClient(HttpClient client)
        {
            _client = client;
        }

        // <inheritdoc />
        public async Task<PlayerRatingResponseContract?> GetRatingAsync(Guid playerId, MatchVariant variant, MatchType type, CancellationToken cancellationToken)
        {
            try
            {
                var url = $"players/{playerId}/rating/{variant}/{type}";
                using var response = await _client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var rating = JsonSerializer.Deserialize<PlayerRatingResponseContract>(responseJson, JsonOptions);
                return rating;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "An error occurred while requesting rating for '{PlayerId}', variant '{Variant}', and type '{Type}'", playerId, variant, type);
                return null;
            }
        }

        // <inheritdoc />
        public async Task<PlayerGamesResponseContract?> GetPlayersGames(Guid playerId, CancellationToken cancellationToken)
        {
            try
            {
                var url = $"players/{playerId}/games";
                using var response = await _client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var games = JsonSerializer.Deserialize<PlayerGamesResponseContract>(responseJson, JsonOptions);
                return games;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "An error occurred while requesting games for '{PlayerId}'", playerId);
                return null;
            }
        }
    }
}
