namespace GammonX.Server.Services
{
    // <inheritdoc />
    public class CancellationTokenServiceImpl : ICancellationTokenService
    {
        private readonly Dictionary<CancellationTokenKey, CancellationTokenSource> _tokenSources = new();
        private readonly object _lock = new();

        // <inheritdoc />
        public void Store(CancellationTokenKey key, CancellationTokenSource cts)
        {
            lock (_lock)
            {
                if (_tokenSources.TryGetValue(key, out var existing))
                {
                    try { existing.Cancel(); }
                    catch (ObjectDisposedException) { }
                }
                _tokenSources[key] = cts;
                // the token removes itself from the registry
                cts.Token.Register(() => RemoveToken(key));
            }
        }

        // <inheritdoc />
        public bool TryGet(CancellationTokenKey key, out CancellationTokenSource? cts)
        {
            lock (_lock)
            {
                return _tokenSources.TryGetValue(key, out cts);
            }
        }

        // <inheritdoc />
        public void Cancel(CancellationTokenKey key)
        {
            lock (_lock)
            {
                if (_tokenSources.TryGetValue(key, out var cts))
                {
                    _tokenSources.Remove(key);
                    try { cts.Cancel(); }
                    catch (ObjectDisposedException) { }
                }
            }
        }

        // <inheritdoc />
        public void CancelForPlayer(Guid playerId, CancellationTokenCategory category)
        {
            lock (_lock)
            {
                var playerTokens = _tokenSources.Where(kvp => kvp.Key.PlayerId == playerId).ToList();
                foreach (var kvp in playerTokens)
                {
                    _tokenSources.Remove(kvp.Key);
                    try { kvp.Value.Cancel(); }
                    catch (ObjectDisposedException) { }
                }
            }
        }

        // <inheritdoc />
        public void CancelForMatch(Guid matchid, CancellationTokenCategory category)
        {
            lock (_lock)
            {
                var matchTokens = _tokenSources.Where(kvp => kvp.Key.MatchId == matchid).ToList();
                foreach (var kvp in matchTokens)
                {
                    _tokenSources.Remove(kvp.Key);
                    try { kvp.Value.Cancel(); }
                    catch (ObjectDisposedException) { }
                }
            }
        }

        private void RemoveToken(CancellationTokenKey key)
        {
            lock (_lock)
            {
                if (_tokenSources.TryGetValue(key, out var cts))
                {
                    _tokenSources.Remove(key);
                }
            }
        }
    }
}
