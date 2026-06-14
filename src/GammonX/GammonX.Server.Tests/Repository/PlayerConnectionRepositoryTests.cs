using GammonX.Server.Services;

using System.Collections.Concurrent;

namespace GammonX.Server.Tests.Repository
{
    public class PlayerConnectionRepositoryTests
    {
        private static PlayerConnectionRepository CreateRepo()
        {
            return new PlayerConnectionRepository();
        }

        [Fact]
        public void CreateAddsNewConnection()
        {
            var repo = CreateRepo();
            var id = Guid.NewGuid();

            var connection = repo.Create(id);

            Assert.NotNull(connection);
            Assert.Equal(id, connection.Id);
        }

        [Fact]
        public void CreateSameIdTwiceThrowsException()
        {
            var repo = CreateRepo();
            var id = Guid.NewGuid();

            repo.Create(id);

            Assert.Throws<InvalidOperationException>(() => repo.Create(id));
        }

        [Fact]
        public void GetExistingconnectionReturnsConnection()
        {
            var repo = CreateRepo();
            var id = Guid.NewGuid();

            var created = repo.Create(id);
            var fetched = repo.Get(id);

            Assert.Same(created, fetched);
        }

        [Fact]
        public void GetNonExistingConnectionReturnsNull()
        {
            var repo = CreateRepo();

            var result = repo.Get(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public void GetOrCreateWhenNotExistingCreatesConnection()
        {
            var repo = CreateRepo();
            var id = Guid.NewGuid();

            var connection = repo.GetOrCreate(id);

            Assert.NotNull(connection);
            Assert.Equal(id, connection.Id);
        }

        [Fact]
        public void GetOrCreateWhenAlreadyExistingReturnsSameInstance()
        {
            var repo = CreateRepo();
            var id = Guid.NewGuid();

            var first = repo.GetOrCreate(id);
            var second = repo.GetOrCreate(id);

            Assert.Same(first, second);
        }

        [Fact]
        public void RemoveExistingConnectionRemovesSuccessfully()
        {
            var repo = CreateRepo();
            var id = Guid.NewGuid();

            repo.Create(id);
            repo.Remove(id);

            Assert.Null(repo.Get(id));
        }

        [Fact]
        public void RemoveNonExistingConnectionThrowsException()
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
                        repo.Create(id);
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
            var id = Guid.NewGuid();

            var repo = new PlayerConnectionRepository();

            var tasks = Enumerable.Range(0, 50)
                .Select(_ => Task.Run(() =>
                {
                    try
                    {
                        return repo.GetOrCreate(id);
                    }
                    catch (InvalidOperationException)
                    {
                        // ingore
                        return null;
                    }
                }))
                .ToArray();

            await Task.WhenAll(tasks);

            // all tasks should return the exact same instance
            var distinctInstances = tasks.Select(t => t.Result).Distinct().ToList();
            Assert.Single(distinctInstances);
        }

        [Fact]
        public async Task ConcurrentAccessAndRemoveDoesNotCorruptState()
        {
            var repo = CreateRepo();
            var id = Guid.NewGuid();

            repo.Create(id);

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
        public void StressTestManyConnectionsConcurrentAccess()
        {
            var repo = CreateRepo();

            var ids = Enumerable.Range(0, 1000)
                .Select(_ => Guid.NewGuid())
                .ToArray();

            Parallel.ForEach(ids, id =>
            {
                repo.GetOrCreate(id);
            });

            Parallel.ForEach(ids, id =>
            {
                var connection = repo.Get(id);
                Assert.NotNull(connection);
            });

            Parallel.ForEach(ids, repo.Remove);

            Assert.All(ids, id => Assert.Null(repo.Get(id)));
        }
    }
}
