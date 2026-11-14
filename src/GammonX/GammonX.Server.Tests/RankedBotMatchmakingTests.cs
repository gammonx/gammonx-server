using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Bot;
using GammonX.Server.Models;
using GammonX.Server.Repository;
using GammonX.Server.Services;

using Moq;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Tests;

public class RankedBotMatchmakingTests
{
    private static readonly QueueKey Key = new(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.FivePointGame);

    [Theory]
    [InlineData(999, BotLevel.Easy)]
    [InlineData(1000, BotLevel.Medium)]
    [InlineData(1199, BotLevel.Medium)]
    [InlineData(1200, BotLevel.Hard)]
    [InlineData(1499, BotLevel.Hard)]
    [InlineData(1500, BotLevel.Expert)]
    public async Task MissingBotRatingsUseFallbackBands(double rating, BotLevel expected)
    {
        // Arrange.
        var repository = new Mock<IRepositoryClient>();
        var service = new RankedMatchmakingService(new PlayerConnectionRepository(), repository.Object);
        var entry = Enqueue(service, rating);

        // Act.
        await service.MatchQueuedPlayersAsync();
        await service.MatchQueuedPlayersAsync();

        // Assert.
        var lobby = GetLobby(service, entry);
        Assert.Equal(expected, lobby.QueueKey.BotLevel);
        Assert.Equal(MatchModus.Ranked, lobby.QueueKey.MatchModus);
        Assert.Equal(Key.MatchVariant, lobby.QueueKey.MatchVariant);
        Assert.Equal(Key.MatchType, lobby.QueueKey.MatchType);
        Assert.Equal(QueueEntryStatus.OpponentFound, lobby.Status);
        Assert.Equal(WellKnownBotPlayers.GetBotPlayerId(expected), lobby.Player2!.Id);
        Assert.True(lobby.Player2.IsBot);
        Assert.Single(service.GetMatchLobbies());
        Assert.Empty(service.GetQueueEntries());
        Assert.True(service.TryFindMatchLobby(lobby.MatchId, out var byMatch));
        Assert.Same(lobby, byMatch);
    }

    [Theory]
    [InlineData(1600, 1600, BotLevel.Easy)]
    [InlineData(1500, 1700, BotLevel.Easy)]
    [InlineData(1200, 1590, BotLevel.Expert)]
    public async Task ClosestRecordedRatingWinsWithStableTies(double easy, double expert, BotLevel expected)
    {
        // Arrange: a nominally easy bot may have the strongest measured rating.
        var repository = new Mock<IRepositoryClient>();
        SetRating(repository, BotLevel.Easy, easy, true);
        SetRating(repository, BotLevel.Expert, expert, true);
        SetRating(repository, BotLevel.Hard, 1600, false);
        SetRating(repository, BotLevel.Medium, 1600, null);
        var service = new RankedMatchmakingService(new PlayerConnectionRepository(), repository.Object);
        var entry = Enqueue(service, 1600);

        // Act.
        await service.MatchQueuedPlayersAsync();

        // Assert.
        Assert.Equal(expected, GetLobby(service, entry).QueueKey.BotLevel);
        repository.Verify(value => value.GetRatingAsync(It.IsAny<Guid>(), Key.MatchVariant, Key.MatchType,
            It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Theory]
    [InlineData(true, BotLevel.Easy)]
    [InlineData(false, BotLevel.Hard)]
    [InlineData(null, BotLevel.Hard)]
    public async Task Recorded1200IsDistinctFromSyntheticOrUnknownRating(bool? recorded, BotLevel expected)
    {
        // Arrange.
        var repository = new Mock<IRepositoryClient>();
        SetRating(repository, BotLevel.Easy, 1200, recorded);
        var service = new RankedMatchmakingService(new PlayerConnectionRepository(), repository.Object);
        var entry = Enqueue(service, 1200);

        // Act.
        await service.MatchQueuedPlayersAsync();

        // Assert.
        Assert.Equal(expected, GetLobby(service, entry).QueueKey.BotLevel);
    }

    [Fact]
    public async Task HumanMatchingIgnoresRequestedBotLevelAndDoesNotReadBotRatings()
    {
        // Arrange.
        var repository = new Mock<IRepositoryClient>();
        var service = new RankedMatchmakingService(new PlayerConnectionRepository(), repository.Object);
        var first = await service.JoinQueueAsync(Guid.NewGuid(), Key with { BotLevel = BotLevel.Easy });
        var second = await service.JoinQueueAsync(Guid.NewGuid(), Key with { BotLevel = BotLevel.Expert });

        // Act.
        await service.MatchQueuedPlayersAsync();

        // Assert.
        Assert.Equal(1200, first.CurrentRating);
        Assert.Equal(BotLevel.Unknown, first.QueueKey.BotLevel);
        Assert.Same(GetLobby(service, first), GetLobby(service, second));
        Assert.Equal(BotLevel.Unknown, GetLobby(service, first).QueueKey.BotLevel);
        repository.Verify(value => value.GetRatingAsync(It.IsAny<Guid>(), It.IsAny<MatchVariant>(), It.IsAny<MatchType>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task IncompatibleCandidateRemainsAvailableToLaterHuman()
    {
        // Arrange.
        var service = new RankedMatchmakingService(new PlayerConnectionRepository(), new SimpleRepositoryClient());
        var first = Enqueue(service, 800);
        var skipped = Enqueue(service, 1800);
        var partner = Enqueue(service, 820);
        var later = Enqueue(service, 1810);

        // Act.
        await service.MatchQueuedPlayersAsync();

        // Assert.
        Assert.Same(GetLobby(service, first), GetLobby(service, partner));
        Assert.Same(GetLobby(service, skipped), GetLobby(service, later));
        Assert.NotEqual(GetLobby(service, first).MatchId, GetLobby(service, later).MatchId);
        Assert.Empty(service.GetQueueEntries());
    }

    [Fact]
    public async Task RatingsAreReusedWithinPassAndRefreshedOnNextPass()
    {
        // Arrange.
        var repository = new Mock<IRepositoryClient>();
        SetRating(repository, BotLevel.Medium, 1200, true);
        var connections = new PlayerConnectionRepository();
        var service = new RankedMatchmakingService(connections, repository.Object);
        var first = Enqueue(service, 800);
        var second = Enqueue(service, 1800);

        // Act.
        await service.MatchQueuedPlayersAsync();
        SetRating(repository, BotLevel.Easy, 1800, true);
        var third = Enqueue(service, 1800);
        await service.MatchQueuedPlayersAsync();

        // Assert.
        Assert.Same(GetLobby(service, first).Player2, GetLobby(service, second).Player2);
        Assert.Equal(BotLevel.Easy, GetLobby(service, third).QueueKey.BotLevel);
        repository.Verify(value => value.GetRatingAsync(It.IsAny<Guid>(), Key.MatchVariant, Key.MatchType,
            It.IsAny<CancellationToken>()), Times.Exactly(8));
    }

    [Fact]
    public async Task CancellationDuringRatingReadDoesNotPublishLobby()
    {
        // Arrange.
        var pending = new TaskCompletionSource<PlayerRatingResponseContract?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new Mock<IRepositoryClient>();
        repository.Setup(value => value.GetRatingAsync(It.IsAny<Guid>(), Key.MatchVariant, Key.MatchType, It.IsAny<CancellationToken>()))
            .Returns(pending.Task);
        var service = new RankedMatchmakingService(new PlayerConnectionRepository(), repository.Object);
        var entry = Enqueue(service, 1200);

        // Act.
        var matching = service.MatchQueuedPlayersAsync();
        await service.LeaveQueueAsync(entry);
        pending.SetResult(null);
        await matching;

        // Assert.
        Assert.Empty(service.GetMatchLobbies());
        Assert.Empty(service.GetQueueEntries());
    }

    [Fact]
    public async Task ConcurrentPassesPublishOnlyOneLobby()
    {
        // Arrange.
        var service = new RankedMatchmakingService(new PlayerConnectionRepository(), new SimpleRepositoryClient());
        Enqueue(service, 1200);

        // Act.
        await Task.WhenAll(service.MatchQueuedPlayersAsync(), service.MatchQueuedPlayersAsync());

        // Assert.
        Assert.Single(service.GetMatchLobbies());
    }

    [Fact]
    public async Task RatingsDoNotLeakAcrossVariantsOrMatchTypes()
    {
        // Arrange.
        var repository = new Mock<IRepositoryClient>();
        SetRating(repository, BotLevel.Easy, 1200, true);
        var service = new RankedMatchmakingService(new PlayerConnectionRepository(), repository.Object);
        var first = Enqueue(service, 1200);
        var otherVariant = await service.JoinQueueAsync(Guid.NewGuid(), Key with { MatchVariant = MatchVariant.Tavla });
        var otherType = await service.JoinQueueAsync(Guid.NewGuid(), Key with { MatchType = MatchType.SevenPointGame });

        // Act.
        await service.MatchQueuedPlayersAsync();

        // Assert.
        Assert.Equal(BotLevel.Easy, GetLobby(service, first).QueueKey.BotLevel);
        Assert.Equal(BotLevel.Hard, GetLobby(service, otherVariant).QueueKey.BotLevel);
        Assert.Equal(BotLevel.Hard, GetLobby(service, otherType).QueueKey.BotLevel);
    }

    [Fact]
    public async Task UnexpectedReadFailureDoesNotStrandHuman()
    {
        // Arrange.
        var repository = new Mock<IRepositoryClient>();
        repository.Setup(value => value.GetRatingAsync(It.IsAny<Guid>(), Key.MatchVariant, Key.MatchType, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("read failed"));
        var service = new RankedMatchmakingService(new PlayerConnectionRepository(), repository.Object);
        var entry = Enqueue(service, 1200);

        // Act.
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MatchQueuedPlayersAsync());
        repository.Setup(value => value.GetRatingAsync(It.IsAny<Guid>(), Key.MatchVariant, Key.MatchType, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerRatingResponseContract?)null);
        await service.MatchQueuedPlayersAsync();

        // Assert.
        Assert.Equal(BotLevel.Hard, GetLobby(service, entry).QueueKey.BotLevel);
        Assert.Empty(service.GetQueueEntries());
    }

    [Theory]
    [InlineData(BotLevel.Easy, "3ab8c995-d304-4680-a106-fbc4c56d4e99")]
    [InlineData(BotLevel.Medium, "94c286c5-ee56-4991-8da0-83e2cb21c440")]
    [InlineData(BotLevel.Hard, "5320200d-2c28-4d02-a2eb-e4e739ae0ac6")]
    [InlineData(BotLevel.Expert, "43557e28-09c4-4625-8c29-9001f0b61ea6")]
    public void BotProfilesKeepReservedIdentities(BotLevel level, string expected)
    {
        // Arrange.
        var expectedId = Guid.Parse(expected);

        // Act.
        var actual = WellKnownBotPlayers.GetBotPlayerId(level);

        // Assert.
        Assert.Equal(expectedId, actual);
        Assert.Throws<ArgumentException>(() => WellKnownBotPlayers.GetBotPlayerId(BotLevel.Unknown));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("human-connection", false)]
    [InlineData("00000000-0000-0000-0000-000000000000", false)]
    [InlineData("3ab8c995-d304-4680-a106-fbc4c56d4e99", false)]
    [InlineData(WellKnownBotPlayers.ConnectionId, true)]
    [InlineData("GAMMONX-BOT", false)]
    public void ConnectionAndMatchPlayerUseReservedBotConnectionId(string? connectionId, bool expected)
    {
        // Arrange.
        var connection = new PlayerConnection(Guid.NewGuid());
        connection.SetConnectionId(connectionId);

        // Act.
        var player = new MatchPlayerModel(connection);

        // Assert.
        Assert.Equal(expected, connection.IsBot);
        Assert.Equal(expected, player.IsBot);
    }

    private static QueueEntry Enqueue(RankedMatchmakingService service, double rating)
    {
        var entry = new QueueEntry(Guid.NewGuid(), Guid.NewGuid(), Key, DateTime.UtcNow, rating);
        service.Enqueue(entry);
        return entry;
    }

    private static MatchLobby GetLobby(RankedMatchmakingService service, QueueEntry entry)
    {
        Assert.True(service.TryFindMatchLobby(entry.Id, out var lobby));
        return Assert.IsType<MatchLobby>(lobby);
    }

    private static void SetRating(Mock<IRepositoryClient> repository, BotLevel level, double rating, bool? recorded)
    {
        repository.Setup(value => value.GetRatingAsync(WellKnownBotPlayers.GetBotPlayerId(level), Key.MatchVariant, Key.MatchType,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerRatingResponseContract { Rating = rating, HasRecordedRating = recorded });
    }
}
