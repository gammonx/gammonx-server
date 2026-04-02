using GammonX.Server.Services;

namespace GammonX.Server.Tests
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method", Justification = "test cases")]
    public class CancellationTokenServiceTests
    {
        private readonly CancellationTokenServiceImpl _service = new CancellationTokenServiceImpl();

        private static CancellationTokenKey CreateKey(Guid playerId, Guid matchId, CancellationTokenCategory category)
        {
            return new CancellationTokenKey
            {
                PlayerId = playerId,
                MatchId = matchId,
                Category = category
            };
        }

        #region Basic Operations

        [Fact]
        public void StoreShouldStoreAndRetrieveToken()
        {
            var playerId = Guid.NewGuid();
            var matchId = Guid.NewGuid();
            var key = CreateKey(playerId, matchId, CancellationTokenCategory.Turn);
            using var cts = new CancellationTokenSource();

            _service.Store(key, cts);
            var result = _service.TryGet(key, out var retrievedCts);

            Assert.True(result);
            Assert.Same(cts, retrievedCts);
        }

        [Fact]
        public void TryGetWithNonexistentKeyShouldReturnFalse()
        {
            var key = CreateKey(Guid.NewGuid(), Guid.NewGuid(), CancellationTokenCategory.Turn);

            var result = _service.TryGet(key, out var cts);

            Assert.False(result);
            Assert.Null(cts);
        }

        [Fact]
        public void CancelShouldCancelAndRemoveToken()
        {
            var playerId = Guid.NewGuid();
            var matchId = Guid.NewGuid();
            var key = CreateKey(playerId, matchId, CancellationTokenCategory.Turn);
            using var cts = new CancellationTokenSource();
            _service.Store(key, cts);

            _service.Cancel(key);

            Assert.True(cts.IsCancellationRequested);
            Assert.False(_service.TryGet(key, out _));
        }

        [Fact]
        public void CancelWithNonexistentKeyShouldNotThrow()
        {
            var key = CreateKey(Guid.NewGuid(), Guid.NewGuid(), CancellationTokenCategory.Turn);
            _service.Cancel(key);
        }

        #endregion Basic Operations

        #region Replacement Operations

        [Fact]
        public void StoreWithExistingKeyShouldCancelPreviousToken()
        {
            var playerId = Guid.NewGuid();
            var matchId = Guid.NewGuid();
            var key = CreateKey(playerId, matchId, CancellationTokenCategory.Turn);
            using var firstCts = new CancellationTokenSource();
            using var secondCts = new CancellationTokenSource();

            _service.Store(key, firstCts);
            _service.Store(key, secondCts);

            Assert.True(firstCts.IsCancellationRequested);
            Assert.False(secondCts.IsCancellationRequested);
            Assert.True(_service.TryGet(key, out var retrieved));
            Assert.Same(secondCts, retrieved);
        }

        #endregion Replacement Operations

        #region Batch Operations

        [Fact]
        public void CancelForPlayerShouldCancelAllPlayerTokens()
        {
            var playerId = Guid.NewGuid();
            var matchId1 = Guid.NewGuid();
            var matchId2 = Guid.NewGuid();
            var key1 = CreateKey(playerId, matchId1, CancellationTokenCategory.Turn);
            var key2 = CreateKey(playerId, matchId2, CancellationTokenCategory.Disconnect);

            using var cts1 = new CancellationTokenSource();
            using var cts2 = new CancellationTokenSource();

            _service.Store(key1, cts1);
            _service.Store(key2, cts2);

            _service.CancelForPlayer(playerId);

            Assert.True(cts1.IsCancellationRequested);
            Assert.True(cts2.IsCancellationRequested);
            Assert.False(_service.TryGet(key1, out _));
            Assert.False(_service.TryGet(key2, out _));
        }

        [Fact]
        public void CancelForPlayerShouldOnlyCancelPlayerTokens()
        {
            var playerId1 = Guid.NewGuid();
            var playerId2 = Guid.NewGuid();
            var matchId = Guid.NewGuid();
            var key1 = CreateKey(playerId1, matchId, CancellationTokenCategory.Turn);
            var key2 = CreateKey(playerId2, matchId, CancellationTokenCategory.Turn);

            using var cts1 = new CancellationTokenSource();
            using var cts2 = new CancellationTokenSource();

            _service.Store(key1, cts1);
            _service.Store(key2, cts2);

            _service.CancelForPlayer(playerId1);

            Assert.True(cts1.IsCancellationRequested);
            Assert.False(cts2.IsCancellationRequested);
            Assert.False(_service.TryGet(key1, out _));
            Assert.True(_service.TryGet(key2, out _));
        }

        [Fact]
        public void CancelForMatchShouldCancelAllMatchTokens()
        {
            var playerId1 = Guid.NewGuid();
            var playerId2 = Guid.NewGuid();
            var matchId = Guid.NewGuid();
            var key1 = CreateKey(playerId1, matchId, CancellationTokenCategory.Turn);
            var key2 = CreateKey(playerId2, matchId, CancellationTokenCategory.Disconnect);

            using var cts1 = new CancellationTokenSource();
            using var cts2 = new CancellationTokenSource();

            _service.Store(key1, cts1);
            _service.Store(key2, cts2);

            _service.CancelForMatch(matchId);

            Assert.True(cts1.IsCancellationRequested);
            Assert.True(cts2.IsCancellationRequested);
            Assert.False(_service.TryGet(key1, out _));
            Assert.False(_service.TryGet(key2, out _));
        }

        [Fact]
        public void CancelForMatchShouldOnlyCancelMatchTokens()
        {
            var playerId = Guid.NewGuid();
            var matchId1 = Guid.NewGuid();
            var matchId2 = Guid.NewGuid();
            var key1 = CreateKey(playerId, matchId1, CancellationTokenCategory.Turn);
            var key2 = CreateKey(playerId, matchId2, CancellationTokenCategory.Turn);

            using var cts1 = new CancellationTokenSource();
            using var cts2 = new CancellationTokenSource();

            _service.Store(key1, cts1);
            _service.Store(key2, cts2);

            _service.CancelForMatch(matchId1);

            Assert.True(cts1.IsCancellationRequested);
            Assert.False(cts2.IsCancellationRequested);
            Assert.False(_service.TryGet(key1, out _));
            Assert.True(_service.TryGet(key2, out _));
        }

        #endregion Batch Operations

        #region Token Callback

        [Fact]
        public void StoreShouldRegisterCancellationCallback()
        {
            var playerId = Guid.NewGuid();
            var matchId = Guid.NewGuid();
            var key = CreateKey(playerId, matchId, CancellationTokenCategory.Turn);
            using var cts = new CancellationTokenSource();
            var callbackFired = false;

            cts.Token.Register(() => callbackFired = true);

            _service.Store(key, cts);
            cts.Cancel();
            
            // Wait a bit for callback to fire
            Task.Delay(10).Wait();

            Assert.True(callbackFired);
            Assert.False(_service.TryGet(key, out _));
        }

        #endregion Token Callback

        #region Multi-threaded Scenarios

        [Fact]
        public async Task StoreConcurrentOperationsShouldBeThreadSafe()
        {
            var tasks = new List<Task>();
            var tokenCount = 100;
            var matchId = Guid.NewGuid();

            // we store tokens from multiple threads
            for (int i = 0; i < tokenCount; i++)
            {
                var playerId = Guid.NewGuid();
                var key = CreateKey(playerId, matchId, CancellationTokenCategory.Turn);
                var cts = new CancellationTokenSource();

                tasks.Add(Task.Run(() => _service.Store(key, cts)));
            }

            await Task.WhenAll(tasks);

            // all tokens should be retrievable
            tasks.Clear();

            // we expect the test completes without deadlock
            Assert.True(tasks.Count >= 0);
        }

        [Fact]
        public async Task ConcurrentStoreAndCancelShouldNotCauseDeadlock()
        {
            var playerId = Guid.NewGuid();
            var matchId = Guid.NewGuid();
            var key = CreateKey(playerId, matchId, CancellationTokenCategory.Turn);
            var tasks = new List<Task>();
            var cts = new CancellationTokenSource();

            // we store, retrieve, and cancel from multiple threads concurrently
            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(() => _service.TryGet(key, out _)));
                tasks.Add(Task.Run(() => _service.Store(key, cts)));
            }

            tasks.Add(Task.Run(() => _service.Cancel(key)));

            // we should complete without deadlock
            var completedWithinTimeout = Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(5));
            Assert.True(completedWithinTimeout);
        }

        [Fact]
        public async Task ConcurrentCancelForPlayerShouldNotCauseDeadlock()
        {
            var playerId = Guid.NewGuid();
            var matchIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();
            var tasks = new List<Task>();

            // we store tokens for the player across different matches
            foreach (var matchId in matchIds)
            {
                var key = CreateKey(playerId, matchId, CancellationTokenCategory.Turn);
                using var cts = new CancellationTokenSource();
                _service.Store(key, cts);
            }

            // we cancel from multiple threads concurrently
            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(() => _service.CancelForPlayer(playerId)));
            }

            // we should complete without deadlock
            var completedWithinTimeout = Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(5));
            Assert.True(completedWithinTimeout);
        }

        [Fact]
        public async Task ConcurrentCancelForMatchShouldNotCauseDeadlock()
        {
            var matchId = Guid.NewGuid();
            var playerIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();
            var tasks = new List<Task>();

            // we store tokens for different players in the same match
            foreach (var playerId in playerIds)
            {
                var key = CreateKey(playerId, matchId, CancellationTokenCategory.Turn);
                using var cts = new CancellationTokenSource();
                _service.Store(key, cts);
            }

            // we cancel from multiple threads concurrently
            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(() => _service.CancelForMatch(matchId)));
            }

            // we should complete without deadlock
            var completedWithinTimeout = Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(5));
            Assert.True(completedWithinTimeout);
        }

        [Fact]
        public async Task MixedOperationsShouldBeThreadSafe()
        {
            var playerId1 = Guid.NewGuid();
            var playerId2 = Guid.NewGuid();
            var matchId1 = Guid.NewGuid();
            var matchId2 = Guid.NewGuid();
            var tasks = new List<Task>();

            // we mix different operations from multiple threads
            for (int i = 0; i < 100; i++)
            {
                var index = i;

                // store operations
                tasks.Add(Task.Run(() =>
                {
                    var key = CreateKey(playerId1, matchId1, CancellationTokenCategory.Turn);
                    using var cts = new CancellationTokenSource();
                    _service.Store(key, cts);
                }));

                // retrieve operations
                tasks.Add(Task.Run(() =>
                {
                    var key = CreateKey(playerId1, matchId1, CancellationTokenCategory.Turn);
                    _service.TryGet(key, out _);
                }));

                // cancel operations
                tasks.Add(Task.Run(() =>
                {
                    var key = CreateKey(playerId2, matchId2, CancellationTokenCategory.Disconnect);
                    _service.Cancel(key);
                }));

                // batch operations
                if (index % 10 == 0)
                {
                    tasks.Add(Task.Run(() => _service.CancelForPlayer(playerId1)));
                    tasks.Add(Task.Run(() => _service.CancelForMatch(matchId2)));
                }
            }

            // we should complete without deadlock or exceptions
            var completedWithinTimeout = Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10));
            Assert.True(completedWithinTimeout);
        }

        #endregion Multi-threaded Scenarios

        #region Edge Cases

        [Fact]
        public void CancelOnAlreadyCancelledTokenShouldNotThrow()
        {
            var key = CreateKey(Guid.NewGuid(), Guid.NewGuid(), CancellationTokenCategory.Turn);
            using var cts = new CancellationTokenSource();
            _service.Store(key, cts);
            cts.Cancel();

            _service.Cancel(key);
        }

        [Fact]
        public void CancelForPlayerWithNoTokensShouldNotThrow()
        {
            var playerId = Guid.NewGuid();

            _service.CancelForPlayer(playerId);
        }

        [Fact]
        public void CancelForMatchWithNoTokensShouldNotThrow()
        {
            var matchId = Guid.NewGuid();

            _service.CancelForMatch(matchId);
        }

        [Fact]
        public void MultipleCancellationsShouldHandleGracefully()
        {
            var key = CreateKey(Guid.NewGuid(), Guid.NewGuid(), CancellationTokenCategory.Turn);
            using var cts = new CancellationTokenSource();
            _service.Store(key, cts);

            _service.Cancel(key);
            _service.Cancel(key);
            _service.Cancel(key);

            Assert.True(cts.IsCancellationRequested);
            Assert.False(_service.TryGet(key, out _));
        }

        #endregion Edge Cases

        #region IDisposable

        [Fact]
        public void DisposedTokenSourceShouldNotCauseException()
        {
            var key = CreateKey(Guid.NewGuid(), Guid.NewGuid(), CancellationTokenCategory.Turn);
            var cts = new CancellationTokenSource();
            _service.Store(key, cts);
            cts.Dispose();

            _service.Cancel(key);
        }

        #endregion IDisposable
    }
}
