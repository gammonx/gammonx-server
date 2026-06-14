using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

namespace GammonX.Server.Bot
{
    /// <summary>
    /// Integration client for the GammonX Mars bot.
    /// </summary>
    public class MarsClient
    {
        private readonly HttpClient _httpClient;

        public MarsClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ResponseContract<MoveEvalPayload>> GetMoveEvalAsync(EvalMoveRequestContract parameters)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            ArgumentNullException.ThrowIfNull(parameters.Board);
            ArgumentNullException.ThrowIfNull(parameters.Rolls);

            if (parameters.BotLevel == BotLevel.Unknown)
            {
                throw new ArgumentException("BotLevel cannot be Unknown.", nameof(parameters));
            }

            var uri = new Uri("api/eval/move", UriKind.Relative);

            using var resp = await _httpClient.PostAsJsonAsync(uri, parameters);
            try
            {
                resp.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                var errorResponse = await resp.Content.ReadAsStringAsync();
                throw new BadHttpRequestException($"Error occurred while sending request: {errorResponse}", ex);
            }

            var response = await resp.Content.ReadAsStringAsync();
            var moveEvalResponse = JsonConvert.DeserializeObject<ResponseContract<MoveEvalPayload>>(response);

            if (moveEvalResponse == null)
                throw new BadHttpRequestException(response);

            return moveEvalResponse;
        }

        public async Task<ResponseContract<CubeEvalPayload>> GetCubeEvalAsync(EvalCubeRequestContract parameters)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            ArgumentNullException.ThrowIfNull(parameters.Board);

            if (parameters.BotLevel == BotLevel.Unknown)
            {
                throw new ArgumentException("BotLevel cannot be Unknown.", nameof(parameters));
            }

            var uri = new Uri("api/eval/cube", UriKind.Relative);

            using var resp = await _httpClient.PostAsJsonAsync(uri, parameters);
            try
            {
                resp.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                var errorResponse = await resp.Content.ReadAsStringAsync();
                throw new BadHttpRequestException($"Error occurred while sending request: {errorResponse}", ex);
            }

            var response = await resp.Content.ReadAsStringAsync();
            var cubeEvalResponse = JsonConvert.DeserializeObject<ResponseContract<CubeEvalPayload>>(response);

            if (cubeEvalResponse == null)
                throw new BadHttpRequestException(response);

            return cubeEvalResponse;
        }

        public async Task<ResponseContract<BoardEvalPayload>> GetBoardEvalAsync(EvalBoardRequestContract parameters)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            ArgumentNullException.ThrowIfNull(parameters.Board);

            var uri = new Uri("api/eval/board", UriKind.Relative);

            using var resp = await _httpClient.PostAsJsonAsync(uri, parameters);
            try
            {
                resp.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                var errorResponse = await resp.Content.ReadAsStringAsync();
                throw new BadHttpRequestException($"Error occurred while sending request: {errorResponse}", ex);
            }

            var response = await resp.Content.ReadAsStringAsync();
            var boardEvalResponse = JsonConvert.DeserializeObject<ResponseContract<BoardEvalPayload>>(response);

            if (boardEvalResponse == null)
                throw new BadHttpRequestException(response);

            return boardEvalResponse;
        }
    }
}
