using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Http;

using Newtonsoft.Json;

namespace GammonX.Server.Bot
{
    /// <summary>
    /// Integration client for the GammonX Mars bot.
    /// </summary>
    public class MarsClient : ResilientHttpClient
    {
        public MarsClient(int? maxAttempts, int? retryBaseDelayMs, HttpMessageHandler? handler = null)
            : base(maxAttempts, retryBaseDelayMs, handler)
        {
            // pass
        }

        public async Task<ResponseContract<MoveEvalPayload>> GetMoveEvalAsync(
            EvalMoveRequestContract parameters, 
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            ArgumentNullException.ThrowIfNull(parameters.Board);
            ArgumentNullException.ThrowIfNull(parameters.Rolls);

            if (parameters.BotLevel == BotLevel.Unknown)
            {
                throw new ArgumentException("BotLevel cannot be Unknown.", nameof(parameters));
            }

            var uri = new Uri("api/eval/move", UriKind.Relative);

            using var resp = await PostAsJsonAndEnsureSuccessAsync(uri, parameters, cancellationToken);

            var response = await resp.Content.ReadAsStringAsync(cancellationToken);
            var moveEvalResponse = JsonConvert.DeserializeObject<ResponseContract<MoveEvalPayload>>(response);

            if (moveEvalResponse == null)
                throw new BadHttpRequestException(response);

            return moveEvalResponse;
        }

        public async Task<ResponseContract<CubeEvalPayload>> GetCubeEvalAsync(
            EvalCubeRequestContract parameters, 
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            ArgumentNullException.ThrowIfNull(parameters.Board);

            if (parameters.BotLevel == BotLevel.Unknown)
            {
                throw new ArgumentException("BotLevel cannot be Unknown.", nameof(parameters));
            }

            var uri = new Uri("api/eval/cube", UriKind.Relative);

            using var resp = await PostAsJsonAndEnsureSuccessAsync(uri, parameters, cancellationToken);

            var response = await resp.Content.ReadAsStringAsync(cancellationToken);
            var cubeEvalResponse = JsonConvert.DeserializeObject<ResponseContract<CubeEvalPayload>>(response);

            if (cubeEvalResponse == null)
                throw new BadHttpRequestException(response);

            return cubeEvalResponse;
        }

        public async Task<ResponseContract<BoardEvalPayload>> GetBoardEvalAsync(
            EvalBoardRequestContract parameters, 
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            ArgumentNullException.ThrowIfNull(parameters.Board);

            var uri = new Uri("api/eval/board", UriKind.Relative);

            using var resp = await PostAsJsonAndEnsureSuccessAsync(uri, parameters, cancellationToken);

            var response = await resp.Content.ReadAsStringAsync(cancellationToken);
            var boardEvalResponse = JsonConvert.DeserializeObject<ResponseContract<BoardEvalPayload>>(response);

            if (boardEvalResponse == null)
                throw new BadHttpRequestException(response);

            return boardEvalResponse;
        }

        private async Task<HttpResponseMessage> PostAsJsonAndEnsureSuccessAsync<TParam>(
            Uri requestUri,
            TParam parameters,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await base.PostAsJsonAsyncWithRetry(requestUri, parameters, cancellationToken);
                try
                {
                    response.EnsureSuccessStatusCode();
                    return response;
                }
                catch (HttpRequestException ex)
                {
                    try
                    {
                        var errorResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                        throw new BadHttpRequestException($"Error occurred while sending request: {errorResponse}", ex);
                    }
                    finally
                    {
                        response.Dispose();
                    }
                }
            }
            catch (RetryableHttpResponseException ex)
            {
                throw new BadHttpRequestException($"Error occurred while sending request: {ex.ResponseBody}", ex);
            }
        }
    }
}
