using GammonX.Engine.Services;

using GammonX.Models.Enums;

using GammonX.Server.Models;
using GammonX.Server.Services;
using Moq;
using System.Collections.Concurrent;

namespace GammonX.Server.Tests.Repository
{
    public class MatchSessionRepositoryTests
    {
        private readonly QueueKey _defaultKey = new(MatchVariant.Tavli, MatchModus.Normal, GammonX.Models.Enums.MatchType.FivePointGame);

        private static MatchSessionRepository CreateRepo()
        {
            var diceFactory = new DiceServiceFactory();
            var gameFactory = new GameSessionFactory(diceFactory);
            var matchFactory = new MatchSessionFactory(gameFactory);
            return new MatchSessionRepository(matchFactory);
        }

        [Fact]
        public void CreateAddsNewSession()
        {
            var repo = CreateRepo();
            var id = Guid.NewGuid();

            var session = repo.Create(id, _defaultKey);

            Assert.NotNull(session);
            Assert.Equal(id, session.Id);
        }

        [Fact]
        public void CreateSameIdTwiceThrowsException()
        {
            var repo = CreateRepo();
            var id = Guid.NewGuid();

            repo.Create(id, _defaultKey);

            Assert.Throws<InvalidOperationException>(() => repo.Create(id, _defaultKey));
        }

        [Fact]
        public void GetExistingSessionReturnsSession()
        {
            var repo = CreateRepo();
            var id = Guid.NewGuid();

            var created = repo.Create(id, _defaultKey);
            var fetched = repo.Get(id);

            Assert.Same(created, fetched);
        }

        [Fact]
        public void GetNonExistingSessionReturnsNull()
        {
            var repo = CreateRepo();

            var result = repo.Get(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public void GetOrCreateWhenNotExistingCreatesSession()
        {
            var repo = CreateRepo();
            var id = Guid.NewGuid();

            var session = repo.GetOrCreate(id, _defaultKey);

            Assert.NotNull(session);
            Assert.Equal(id, session.Id);
        }

        [Fact]
        public void GetOrCreateWhenAlreadyExistingReturnsSameInstance()
        {
            var repo = CreateRepo();
            var id = Guid.NewGuid();

            var first = repo.GetOrCreate(id, _defaultKey);
            var second = repo.GetOrCreate(id, _defaultKey);

            Assert.Same(first, second);
        }

        [Fact]
        public void RemoveExistingSessionRemovesSuccessfully()
        {
            var repo = CreateRepo();
            var id = Guid.NewGuid();

            repo.Create(id, _defaultKey);
            repo.Remove(id);

            Assert.Null(repo.Get(id));
        }

        [Fact]
        public void RemoveNonExistingSessionThrowsException()
        {
            var repo = CreateRepo();

            Assert.Throws<KeyNotFoundException>(() => repo.Remove(Guid.NewGuid()));
        }

        [Fact]
        public async Task ConcurrentCreateSameIdOnlyOneSucceeds()
        {
            var repo = CreateRepo();
            var id = Guid.NewGuid();

            var exceptions = new ConcurrentBag<Exception>();
            var tasks = new List<Task>();

            for (int i = 0; i < 20; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        repo.Create(id, _defaultKey);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                },
                TestContext.Current.CancellationToken));
            }

            await Task.WhenAll(tasks.ToArray());

            // exactly one should succeed, rest should fail
            Assert.True(exceptions.Count >= 19);
            Assert.NotNull(repo.Get(id));
        }

        [Fact]
        public async Task ConcurrentGetOrCreateCreatesOnlyOneInstance()
        {
            var mockFactory = new Mock<IMatchSessionFactory>();
            var id = Guid.NewGuid();

            mockFactory
                .Setup(f => f.Create(It.IsAny<Guid>(), It.IsAny<QueueKey>()))
                .Returns<Guid, QueueKey>((i, k) => new TavliMatchSession(i, k, new GameSessionFactory(new DiceServiceFactory())));

            var repo = new MatchSessionRepository(mockFactory.Object);

            var tasks = Enumerable.Range(0, 50)
                .Select(_ => Task.Run(() =>
                {
                    try
                    {
                        return repo.GetOrCreate(id, _defaultKey);
                    }
                    catch (InvalidOperationException)
                    {
                        // ingore
                        return null;
                    }
                }))
                .ToArray();

            await Task.WhenAll(tasks);

            mockFactory.Verify(
                f => f.Create(id, _defaultKey),
                Times.Once
            );

            // all tasks should return the exact same instance
            var distinctInstances = tasks.Select(t => t.Result).Distinct().ToList();
            Assert.Single(distinctInstances);
        }

        [Fact]
        public async Task ConcurrentAccessAndRemoveDoesNotCorruptState()
        {
            var repo = CreateRepo();
            var id = Guid.NewGuid();

            repo.Create(id, _defaultKey);

            var tasks = new List<Task>();

            // mix reads and removes
            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(() => repo.Get(id)));

                tasks.Add(Task.Run(() =>
                {
                    try { repo.Remove(id); }
                    catch { /* expected sometimes */ }
                },
                TestContext.Current.CancellationToken));
            }

            await Task.WhenAll(tasks.ToArray());

            // final state must be either present or removed, but not corrupted
            var final = repo.Get(id);
            Assert.True(final == null || final.Id == id);
        }

        [Fact]
        public void StressTestManySessionsConcurrentAccess()
        {
            var repo = CreateRepo();

            var ids = Enumerable.Range(0, 1000)
                .Select(_ => Guid.NewGuid())
                .ToArray();

            Parallel.ForEach(ids, id =>
            {
                repo.GetOrCreate(id, _defaultKey);
            });

            Parallel.ForEach(ids, id =>
            {
                var session = repo.Get(id);
                Assert.NotNull(session);
            });

            Parallel.ForEach(ids, repo.Remove);

            Assert.All(ids, id => Assert.Null(repo.Get(id)));
        }
    }
}
