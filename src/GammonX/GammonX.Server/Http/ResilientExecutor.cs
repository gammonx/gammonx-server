
using System.Net;

using Amazon.Runtime;

namespace GammonX.Server.Http
{
    /// <summary>
    /// Provides resilient execution of operations with retry logic for transient failures.
    /// </summary>
    public class ResilientExecutor
    {
        private const int DefaultMaxAttempts = 3;
        private const int DefaultRetryBaseDelayMilliseconds = 1000;

        private readonly int _maxAttempts;

        private readonly TimeSpan _retryBaseDelay;

        private readonly Func<Exception, bool> _isTransient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResilientExecutor"/> class with the specified retry settings.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of retry attempts for operations.</param>
        /// <param name="retryBaseDelayMs">The base delay in milliseconds between retry attempts.</param>
        /// <param name="isTransient">A function to determine if an exception is transient and should be retried.</param>
        public ResilientExecutor(int? maxAttempts, int? retryBaseDelayMs, Func<Exception, bool>? isTransient)
        {
            _maxAttempts = maxAttempts ?? DefaultMaxAttempts;
            if (_maxAttempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "At least one attempt is required.");
            }

            var retryBaseDelayMilliseconds = retryBaseDelayMs ?? DefaultRetryBaseDelayMilliseconds;
            if (retryBaseDelayMilliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retryBaseDelayMs), "The retry delay cannot be negative.");
            }

            _retryBaseDelay = TimeSpan.FromMilliseconds(retryBaseDelayMilliseconds);
            _isTransient = isTransient ?? IsTransient;
        }

        /// <summary>
        /// Executes the specified operation with retry logic for transient failures.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the operation.</typeparam>
        /// <param name="operation">The asynchronous operation to be executed with retry logic.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation, containing the result of type <typeparamref name="T"/>.</returns>
        public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(operation);
            cancellationToken.ThrowIfCancellationRequested();

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested && _isTransient(ex) && attempt < _maxAttempts)
                {
                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                }
            }
        }

        private async Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
        {
            if (_retryBaseDelay == TimeSpan.Zero)
            {
                return;
            }

            var delay = TimeSpan.FromMilliseconds(_retryBaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
            await Task.Delay(delay, cancellationToken);
        }

        private static bool IsTransient(Exception exception)
        {
            if (exception is TaskCanceledException)
            {
                return true;
            }

            if (exception is TimeoutException or IOException)
            {
                return true;
            }

            if (exception is HttpRequestException httpRequestException)
            {
                return httpRequestException.StatusCode is null
                    || httpRequestException.StatusCode == HttpStatusCode.RequestTimeout
                    || httpRequestException.StatusCode == HttpStatusCode.TooManyRequests
                    || (int)httpRequestException.StatusCode >= 500;
            }

            if (exception is not AmazonServiceException serviceException)
            {
                return false;
            }

            var statusCode = (int)serviceException.StatusCode;
            return statusCode == (int)HttpStatusCode.RequestTimeout
                || statusCode == (int)HttpStatusCode.TooManyRequests
                || statusCode >= 500
                || serviceException.ErrorCode is "RequestThrottled" or "ThrottlingException";
        }
    }
}