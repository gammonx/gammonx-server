namespace GammonX.Server.Tests.Http
{
    internal sealed class ScriptedHttpMessageHandler(
            params Func<CancellationToken, Task<HttpResponseMessage>>[] responses) : HttpMessageHandler
    {
        private readonly Queue<Func<CancellationToken, Task<HttpResponseMessage>>> _responses = new(responses);

        public List<Uri?> RequestUris { get; } = [];

        public List<string> RequestBodies { get; } = [];

        public int RequestCount => RequestUris.Count;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri);
            RequestBodies.Add(request.Content == null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No scripted HTTP response remains.");
            }

            return await _responses.Dequeue()(cancellationToken);
        }
    }
}