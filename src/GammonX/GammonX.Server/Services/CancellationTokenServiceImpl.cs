namespace GammonX.Server.Services
{
    // <inheritdoc />
    public class CancellationTokenServiceImpl : ICancellationTokenService
    {
        private sealed class Entry(CancellationTokenSource source)
        {
            internal CancellationTokenSource Source { get; } = source;
            internal CancellationTokenRegistration Removal { get; set; }
        }

        private readonly Dictionary<CancellationTokenKey, Entry> _tokenSources = new();

        private readonly Lock _lock = new();

        internal int Count
        {
            get
            {
                lock (_lock)
                {
                    return _tokenSources.Count;
                }
            }
        }

        // <inheritdoc />
        public void Store(CancellationTokenKey key, CancellationTokenSource cts)
        {
            Entry? existing;
            lock (_lock)
            {
                _tokenSources.TryGetValue(key, out existing);
                if (ReferenceEquals(existing?.Source, cts))
                {
                    return;
                }

                var token = cts.Token;
                var entry = new Entry(cts);
                _tokenSources[key] = entry;
                entry.Removal = token.UnsafeRegister(_ => RemoveToken(key, entry), null);
            }

            CancelSource(existing);
        }

        // <inheritdoc />
        public bool TryGet(CancellationTokenKey key, out CancellationTokenSource? cts)
        {
            lock (_lock)
            {
                var found = _tokenSources.TryGetValue(key, out var entry);
                cts = entry?.Source;
                return found;
            }
        }

        // <inheritdoc />
        public void Cancel(CancellationTokenKey key)
        {
            Entry? source;
            lock (_lock)
            {
                _tokenSources.Remove(key, out source);
            }
            CancelSource(source);
        }

        // <inheritdoc />
        public bool TryCancel(CancellationTokenKey key, CancellationTokenSource expected)
        {
            Entry? current;
            lock (_lock)
            {
                if (!_tokenSources.TryGetValue(key, out current) || !ReferenceEquals(current.Source, expected) || expected.IsCancellationRequested)
                {
                    return false;
                }

                _tokenSources.Remove(key);
            }
            return CancelSource(current);
        }

        // <inheritdoc />
        public void CancelForPlayer(Guid playerId, CancellationTokenCategory category)
        {
            CancelMatching(key => key.PlayerId == playerId && key.Category == category);
        }

        // <inheritdoc />
        public void CancelForMatch(Guid matchid, CancellationTokenCategory category)
        {
            CancelMatching(key => key.MatchId == matchid && key.Category == category);
        }

        private void CancelMatching(Func<CancellationTokenKey, bool> matches)
        {
            KeyValuePair<CancellationTokenKey, Entry>[] removed;
            lock (_lock)
            {
                removed = _tokenSources.Where(pair => matches(pair.Key)).ToArray();
                foreach (var pair in removed)
                {
                    _tokenSources.Remove(pair.Key);
                }
            }

            foreach (var pair in removed)
            {
                CancelSource(pair.Value);
            }
        }

        private static bool CancelSource(Entry? entry)
        {
            if (entry == null)
            {
                return false;
            }

            entry.Removal.Unregister();
            try
            {
                entry.Source.Cancel();
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (AggregateException ex)
            {
                // The source is canceled. One faulty listener must not strand other detached timers.
                Serilog.Log.Error(ex, "A deadline cancellation callback failed.");
                return true;
            }
        }

        private void RemoveToken(CancellationTokenKey key, Entry expected)
        {
            lock (_lock)
            {
                if (_tokenSources.TryGetValue(key, out var cts) && ReferenceEquals(cts, expected))
                {
                    _tokenSources.Remove(key);
                }
            }
        }
    }
}
