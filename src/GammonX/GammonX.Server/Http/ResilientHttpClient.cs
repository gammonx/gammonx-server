using System.Net;

namespace GammonX.Server.Http
{
    /// <summary>
    /// A base HTTP client that provides resilient execution with retry logic for HTTP requests.
    /// </summary>
    public abstract class ResilientHttpClient : HttpClient
    {
        private readonly ResilientExecutor _resilientExecutor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResilientHttpClient"/> class with the specified retry settings.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts for HTTP requests.</param>
        /// <param name="retryBaseDelayMs">The base delay in milliseconds between retry attempts.</param>
        public ResilientHttpClient(int? maxAttempts, int? retryBaseDelayMs, HttpMessageHandler? handler = null)
            : base(handler ?? new HttpClientHandler())
        {
            _resilientExecutor = new ResilientExecutor(maxAttempts, retryBaseDelayMs, null);
        }

        /// <summary>
        /// Sends a POST request with a JSON body and retries on failure.
        /// </summary>
        /// <typeparam name="TParam">The type of the value to be sent as JSON in the request body.</typeparam>
        /// <param name="requestUri">The URI of the request.</param>
        /// <param name="value">The value to be sent as JSON in the request body.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The HTTP response message resulting from the POST request.</returns>
        public Task<HttpResponseMessage> PostAsJsonAsyncWithRetry<TParam>(Uri? requestUri, TParam value, CancellationToken cancellationToken)
        {
            return _resilientExecutor.ExecuteWithRetryAsync(async () =>
            {
                var response = await this.PostAsJsonAsync(requestUri, value, cancellationToken);
                if (IsRetryableStatusCode(response.StatusCode))
                {
                    try
                    {
                        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        throw new RetryableHttpResponseException(response.StatusCode, responseBody);
                    }
                    finally
                    {
                        response.Dispose();
                    }
                }

                return response;
            }, cancellationToken);
        }

        private static bool IsRetryableStatusCode(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.RequestTimeout
                || statusCode == HttpStatusCode.TooManyRequests
                || (int)statusCode >= 500;
        }
    }

    internal sealed class RetryableHttpResponseException : HttpRequestException
    {
        public RetryableHttpResponseException(HttpStatusCode statusCode, string responseBody)
            : base($"The remote server returned HTTP {(int)statusCode} ({statusCode}).", null, statusCode)
        {
            ResponseBody = responseBody;
        }

        public string ResponseBody { get; }
    }
}