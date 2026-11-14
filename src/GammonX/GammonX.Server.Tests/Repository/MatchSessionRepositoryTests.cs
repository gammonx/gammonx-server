using GammonX.Engine.Services;
using GammonX.Models.Enums;
using GammonX.Server.Models;
using GammonX.Server.Services;
using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Tests.Repository
{
    public class MatchSessionRepositoryTests
    {
        private readonly QueueKey _defaultKey = new(MatchVariant.Tavli, MatchModus.Normal, MatchType.FivePointGame);

        private static MatchSessionRepository CreateRepo()
        {
            return new MatchSessionRepository(new MatchSessionFactory(new GameSessionFactory(new DiceServiceFactory())));
        }

        [Fact]
        public async Task WaitingOperationReadsCommittedStateBeforeApplyingItsUpdate()
        {
            // Arrange
            var repo = CreateRepo();
            var id = Guid.NewGuid();
            var firstLease = await repo.AcquireStateAsync(id);
            var first = firstLease.Create(_defaultKey);
            var waitingLease = repo.AcquireStateAsync(id);

            // Act
            Assert.False(waitingLease.IsCompleted);
            first.Player1.AcceptNextGame();
            Assert.True(firstLease.Commit());
            await firstLease.DisposeAsync();
            await using (var secondLease = await waitingLease.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken))
            {
                secondLease.Get()!.Player2.AcceptNextGame();
                Assert.True(secondLease.Commit());
            }

            // Assert
            var stored = repo.Get(id)!;
            Assert.True(stored.Player1.NextGameAccepted);
            Assert.True(stored.Player2.NextGameAccepted);
        }

        [Fact]
        public async Task RemovalDoesNotSplitLockBetweenWaitingOperations()
        {
            // Arrange
            var repo = CreateRepo();
            var id = Guid.NewGuid();
            var firstLease = await repo.AcquireStateAsync(id);
            firstLease.Create(_defaultKey);
            var waitingLease = repo.AcquireStateAsync(id);

            // Act
            Assert.True(firstLease.TryRemove());
            await firstLease.DisposeAsync();
            var secondLease = await waitingLease.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            var thirdLease = repo.AcquireStateAsync(id);

            // Assert
            Assert.Null(secondLease.Get());
            Assert.False(thirdLease.IsCompleted);
            await secondLease.DisposeAsync();
            await using var finalLease = await thirdLease.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task DifferentMatchesCanBeLockedIndependently()
        {
            // Arrange
            var repo = CreateRepo();
            await using var firstLease = await repo.AcquireStateAsync(Guid.NewGuid());

            // Act
            var secondLease = repo.AcquireStateAsync(Guid.NewGuid());

            // Assert
            Assert.True(secondLease.IsCompletedSuccessfully);
            await using var lease = await secondLease;
        }

        [Fact]
        public async Task CreateAndGetOrCreateUseOnlyTheLeasedMatch()
        {
            // Arrange
            var repo = CreateRepo();
            var id = Guid.NewGuid();
            await using var lease = await repo.AcquireStateAsync(id);

            // Act
            var created = lease.Create(_defaultKey);

            // Assert
            Assert.Equal(id, created.Id);
            Assert.Same(created, lease.Get());
            Assert.Same(created, lease.GetOrCreate(_defaultKey));
            Assert.NotSame(created, repo.Get(id));
            Assert.Throws<InvalidOperationException>(() => lease.Create(_defaultKey));
        }

        [Fact]
        public async Task GetOrCreateCreatesMissingMatch()
        {
            // Arrange
            var repo = CreateRepo();
            var id = Guid.NewGuid();
            await using var lease = await repo.AcquireStateAsync(id);

            // Act
            var session = lease.GetOrCreate(_defaultKey);

            // Assert
            Assert.Equal(id, session.Id);
            Assert.NotNull(repo.Get(id));
        }

        [Fact]
        public async Task MissingMatchCannotBeCommittedOrRemoved()
        {
            // Arrange
            var repo = CreateRepo();
            var id = Guid.NewGuid();
            await using var lease = await repo.AcquireStateAsync(id);

            // Act / Assert
            Assert.Null(lease.Get());
            Assert.False(lease.Commit());
            Assert.False(lease.TryRemove());
            Assert.Throws<KeyNotFoundException>(() => lease.Remove());
            Assert.Null(repo.Get(id));
        }

        [Fact]
        public async Task RemovingMatchDiscardsWorkingSnapshot()
        {
            // Arrange
            var repo = CreateRepo();
            var id = Guid.NewGuid();
            await using var lease = await repo.AcquireStateAsync(id);
            var session = lease.Create(_defaultKey);

            // Act
            lease.Remove();
            session.Player1.AcceptNextGame();

            // Assert
            Assert.Null(lease.Get());
            Assert.False(lease.Commit());
            Assert.Null(repo.Get(id));
        }

        [Fact]
        public async Task UncommittedChangesDoNotChangeStoredSession()
        {
            // Arrange
            var repo = CreateRepo();
            var id = Guid.NewGuid();
            await using var lease = await repo.AcquireStateAsync(id);
            var session = lease.Create(_defaultKey);

            // Act
            session.Player1.SetStartDiceRoll(2);
            session.Player1.AcceptNextGame();

            // Assert
            Assert.Null(repo.Get(id)!.Player1.StartDiceRoll);
            Assert.False(repo.Get(id)!.Player1.NextGameAccepted);
        }

        [Fact]
        public async Task CommitCapturesSnapshotAndSupportsFurtherUpdatesWithinLease()
        {
            // Arrange
            var repo = CreateRepo();
            var id = Guid.NewGuid();
            await using var lease = await repo.AcquireStateAsync(id);
            var session = lease.Create(_defaultKey);
            session.Player1.SetStartDiceRoll(2);

            // Act
            Assert.True(lease.Commit());
            session.Player1.SetStartDiceRoll(6);

            // Assert
            Assert.Equal(2, repo.Get(id)!.Player1.StartDiceRoll);
            Assert.True(lease.Commit());
            Assert.Equal(6, repo.Get(id)!.Player1.StartDiceRoll);
        }

        [Fact]
        public async Task DetachedAndPreviousLeaseSnapshotsCannotOverwriteCurrentState()
        {
            // Arrange
            var repo = CreateRepo();
            var id = Guid.NewGuid();
            var firstLease = await repo.AcquireStateAsync(id);
            var oldSnapshot = firstLease.Create(_defaultKey);
            await firstLease.DisposeAsync();
            var detached = repo.Get(id)!;
            await using var currentLease = await repo.AcquireStateAsync(id);
            currentLease.Get()!.Player1.SetStartDiceRoll(2);

            // Act
            detached.Player1.SetStartDiceRoll(5);
            oldSnapshot.Player1.SetStartDiceRoll(6);
            Assert.True(currentLease.Commit());

            // Assert
            Assert.Throws<ObjectDisposedException>(() => firstLease.Commit());
            Assert.Equal(2, repo.Get(id)!.Player1.StartDiceRoll);
        }

        [Fact]
        public async Task DisposedLeaseRejectsEveryOperationAndDoubleDisposalDoesNotUnlockNextOwner()
        {
            // Arrange
            var repo = CreateRepo();
            var id = Guid.NewGuid();
            var oldLease = await repo.AcquireStateAsync(id);
            oldLease.Create(_defaultKey);
            await oldLease.DisposeAsync();
            var currentLease = await repo.AcquireStateAsync(id);
            var waitingLease = repo.AcquireStateAsync(id);

            // Act
            await oldLease.DisposeAsync();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => oldLease.Get());
            Assert.Throws<ObjectDisposedException>(() => oldLease.Create(_defaultKey));
            Assert.Throws<ObjectDisposedException>(() => oldLease.GetOrCreate(_defaultKey));
            Assert.Throws<ObjectDisposedException>(() => oldLease.Commit());
            Assert.Throws<ObjectDisposedException>(() => oldLease.TryRemove());
            Assert.Throws<ObjectDisposedException>(() => oldLease.Remove());
            Assert.False(waitingLease.IsCompleted);
            await currentLease.DisposeAsync();
            await using var finalLease = await waitingLease.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task ExceptionReleasesLeaseWithoutPublishingChanges()
        {
            // Arrange
            var repo = CreateRepo();
            var id = Guid.NewGuid();

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await using var lease = await repo.AcquireStateAsync(id);
                lease.Create(_defaultKey).Player1.AcceptNextGame();
                throw new InvalidOperationException("Failed command.");
            });

            // Assert
            await using var next = await repo.AcquireStateAsync(id).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.False(next.Get()!.Player1.NextGameAccepted);
        }

        [Fact]
        public async Task ConcurrentCreateSameIdOnlyOneSucceeds()
        {
            // Arrange
            var repo = CreateRepo();
            var id = Guid.NewGuid();

            // Act
            var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(async _ =>
            {
                await using var lease = await repo.AcquireStateAsync(id);
                try
                {
                    lease.Create(_defaultKey);
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }));

            // Assert
            Assert.Single(results, value => value);
            Assert.NotNull(repo.Get(id));
        }

        [Fact]
        public async Task GetPlayersMatchReturnsDetachedSnapshot()
        {
            // Arrange
            var repo = CreateRepo();
            var player = new PlayerConnection(Guid.NewGuid());
            player.SetConnectionId("human");
            await using var lease = await repo.AcquireStateAsync(Guid.NewGuid());
            var session = lease.Create(_defaultKey);
            session.JoinSession(player);
            Assert.True(lease.Commit());

            // Act
            var found = repo.GetPlayersMatch(player.Id);

            // Assert
            Assert.NotNull(found);
            Assert.Equal(session.Id, found.Id);
            Assert.NotSame(session, found);
            Assert.Null(repo.GetPlayersMatch(Guid.NewGuid()));
        }
    }
}
