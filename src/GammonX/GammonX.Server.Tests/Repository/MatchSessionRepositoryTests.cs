using GammonX.Engine.Services;
using GammonX.Engine.History;

using GammonX.Models.Enums;

using GammonX.Server.Models;
using GammonX.Server.Services;

using Moq;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Tests.Repository
{
    public class MatchSessionRepositoryTests
    {
        private readonly QueueKey _defaultKey = new(MatchVariant.Tavli, MatchModus.Normal, MatchType.FivePointGame);

        [Theory]
        [InlineData(MatchVariant.Backgammon)]
        [InlineData(MatchVariant.Tavla)]
        [InlineData(MatchVariant.Tavli)]
        public async Task CompletedRoundsAreSharedUntilExplicitlyAccessed(MatchVariant variant)
        {
            // Arrange
            var repo = CreateRepo();
            var id = Guid.NewGuid();
            var historyEvent = new Mock<IHistoryEvent>();
            var cloneable = historyEvent.As<ICloneable>();
            cloneable.Setup(value => value.Clone()).Returns(historyEvent.Object);
            await using (var lease = await repo.AcquireStateAsync(id))
            {
                var match = lease.Create(new QueueKey(variant, MatchModus.Normal, MatchType.FivePointGame));
                match.JoinSession(new PlayerConnection(Guid.NewGuid(), "one"));
                match.JoinSession(new PlayerConnection(Guid.NewGuid(), "two"));
                match.Player1.AcceptNextGame();
                match.Player2.AcceptNextGame();
                var round = match.StartMatch(match.Player1.Id);
                ((IEditableBoardHistory)round.BoardModel.History).Add(historyEvent.Object);
                match.ResignGame(match.Player2.Id);
                match.Player1.AcceptNextGame();
                match.Player2.AcceptNextGame();
                match.StartNextGame(match.Player1.Id);
                Assert.True(lease.Commit());
            }
            cloneable.Invocations.Clear();

            // Act
            await using (var lease = await repo.AcquireStateAsync(id))
            {
                Assert.NotNull(lease.Get());
                Assert.True(lease.Commit());
            }

            // Assert
            cloneable.Verify(value => value.Clone(), Times.Never);
            var snapshot = repo.Get(id)!;
            var completed = snapshot.GetGameSession(1)!;
            var completedBoard = completed.BoardModel;
            cloneable.Verify(value => value.Clone(), Times.Once);
            completedBoard.Fields[0] = 99;
            completedBoard.History.Events.Clear();
            Assert.NotEqual(99, repo.Get(id)!.GetGameSession(1)!.BoardModel.Fields[0]);
            Assert.Single(repo.Get(id)!.GetGameSession(1)!.BoardModel.History.Events);
        }

        [Theory]
        [InlineData(MatchVariant.Backgammon, false)]
        [InlineData(MatchVariant.Backgammon, true)]
        [InlineData(MatchVariant.Tavla, false)]
        [InlineData(MatchVariant.Tavla, true)]
        [InlineData(MatchVariant.Tavli, false)]
        [InlineData(MatchVariant.Tavli, true)]
        public async Task StateQueriesDoNotCloneMatchAndReturnDetachedPayloads(MatchVariant variant, bool completed)
        {
            // Arrange
            var repo = CreateRepo();
            var id = Guid.NewGuid();
            var playerId = Guid.NewGuid();
            await using (var lease = await repo.AcquireStateAsync(id))
            {
                var match = lease.Create(new QueueKey(variant, MatchModus.Normal, MatchType.FivePointGame));
                match.JoinSession(new PlayerConnection(playerId, "one"));
                match.JoinSession(new PlayerConnection(Guid.NewGuid(), "two"));
                match.Player1.AcceptNextGame();
                match.Player2.AcceptNextGame();
                var round = match.StartMatch(match.Player1.Id);
                round.SetDiceRolls([2, 3], true);
                var historyEvent = new Mock<IHistoryEvent>();
                historyEvent.As<ICloneable>().Setup(value => value.Clone()).Returns(historyEvent.Object);
                ((IEditableBoardHistory)round.BoardModel.History).Add(historyEvent.Object);
                if (completed)
                {
                    round.StopGame(GammonX.Models.GameResultModel.Draw());
                }
                Assert.True(lease.Commit());
                historyEvent.As<ICloneable>().Setup(value => value.Clone()).Throws(new InvalidOperationException("Query cloned history"));
            }

            // Act
            await using var query = await repo.AcquireStateAsync(id);
            var game = query.ReadGameState(playerId)!;
            var matchState = query.ReadMatchState(playerId)!;
            var originalField = game.BoardState.Fields[0];
            var originalMove = game.MoveSequences[0].Moves[0].From;
            game.BoardState.Fields[0] = 99;
            game.DiceRolls[0].Used = true;
            game.MoveSequences[0].Moves[0].From = 99;
            matchState.GameRounds![0].Points = 99;

            // Assert
            var reread = query.ReadGameState(playerId)!;
            Assert.Equal(originalField, reread.BoardState.Fields[0]);
            Assert.False(reread.DiceRolls[0].Used);
            Assert.Equal(originalMove, reread.MoveSequences[0].Moves[0].From);
            Assert.NotEqual(99, query.ReadMatchState(playerId)!.GameRounds![0].Points);
            Assert.False(query.Commit()); // A state query never loads a working match.
        }

        private static MatchSessionRepository CreateRepo()
        {
            return new MatchSessionRepository(new MatchSessionFactory(new GameSessionFactory(new DiceServiceFactory())));
        }

        [Fact]
        public async Task PlayerHelpersObserveWorkingStateAndRejectDisposedLeases()
        {
            // Arrange
            var repo = CreateRepo();
            var id = Guid.NewGuid();
            var player1 = Guid.NewGuid();
            var player2 = Guid.NewGuid();
            await using (var setup = await repo.AcquireStateAsync(id))
            {
                var match = setup.Create(_defaultKey);
                match.JoinSession(new PlayerConnection(player1, "one"));
                match.JoinSession(new PlayerConnection(player2, "two"));
                Assert.True(setup.Commit());
            }

            // Act: scalar reads do not load a working copy; subsequent updates are visible.
            var lease = await repo.AcquireStateAsync(id);
            Assert.Equal(player1, lease.GetCallingPlayerId("one"));
            Assert.Equal(player2, lease.GetOtherPlayerId(player1));
            Assert.Equal("two", lease.GetPlayerConnectionId(player2));
            Assert.True(lease.IsWhite(player1));
            Assert.False(lease.IsWhite(player2));
            Assert.False(lease.IsBotPlayer(player2));
            Assert.False(lease.Commit());
            Assert.True(lease.TryRemove());
            Assert.Throws<InvalidOperationException>(() => lease.GetCallingPlayerId("one"));
            Assert.Throws<InvalidOperationException>(() => lease.GetOtherPlayerId(player1));
            Assert.Throws<InvalidOperationException>(() => lease.IsBotPlayer(player2));
            await lease.DisposeAsync();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => lease.GetCallingPlayerId("one"));
            Assert.Throws<ObjectDisposedException>(() => lease.GetOtherPlayerId(player1));
            Assert.Throws<ObjectDisposedException>(() => lease.GetPlayerConnectionId(player1));
            Assert.Throws<ObjectDisposedException>(() => lease.IsWhite(player1));
            Assert.Throws<ObjectDisposedException>(() => lease.IsBotPlayer(player1));
            Assert.Null(repo.Get(id));
        }

        [Fact]
        public async Task CompletedMatchQueriesDoNotSetStoredEndTime()
        {
            // Arrange
            var repo = CreateRepo();
            var id = Guid.NewGuid();
            var player = Guid.NewGuid();
            await using (var setup = await repo.AcquireStateAsync(id))
            {
                var match = setup.Create(_defaultKey);
                match.JoinSession(new PlayerConnection(player, "one"));
                match.JoinSession(new PlayerConnection(Guid.NewGuid(), "two"));
                match.Player1.AcceptNextGame();
                match.Player2.AcceptNextGame();
                var round = match.StartMatch(player);
                round.StopGame(GammonX.Models.GameResultModel.Draw());
                match.Player1.Points = 5;
                Assert.True(setup.Commit());
            }

            // Act
            await using (var query = await repo.AcquireStateAsync(id))
            {
                Assert.NotNull(query.ReadGameState(player));
                Assert.NotNull(query.ReadMatchState(player));
            }

            // Assert
            Assert.Null(repo.Get(id)!.EndedAt);
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
