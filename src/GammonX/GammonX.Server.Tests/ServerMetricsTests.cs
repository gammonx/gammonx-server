using System.Buffers;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text.Json.Serialization;

using GammonX.Engine.Services;
using GammonX.Models.Enums;
using GammonX.Models.Helpers;
using GammonX.Diagnostics.Server;
using GammonX.Server.Models;
using GammonX.Server.Services;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Moq;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Tests;

[CollectionDefinition("Server metrics", DisableParallelization = true)]
public class ServerMetricsCollection;

[Collection("Server metrics")]
public class ServerMetricsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProtocolCountsActualUtf8BytesWithoutSerializingTwice(bool getBytes)
    {
        // Arrange
        using var capture = new Capture();
        var options = new JsonHubProtocolOptions();
        options.PayloadSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
        var measured = new MetricsJsonHubProtocol(Options.Create(options));
        var reference = new JsonHubProtocol(Options.Create(options));
        var payload = new CountingPayload();
        var message = new InvocationMessage("Test", [payload]);
        var expected = reference.GetMessageBytes(new InvocationMessage("Test", [new CountingPayload()]));

        // Act
        byte[] actual;
        if (getBytes)
        {
            actual = measured.GetMessageBytes(message).ToArray();
        }
        else
        {
            var output = new ArrayBufferWriter<byte>();
            measured.WriteMessage(message, output);
            actual = output.WrittenSpan.ToArray();
        }

        // Assert
        Assert.Equal(expected.ToArray(), actual);
        Assert.Equal(1, payload.Reads);
        var sample = Assert.Single(capture.Read("gammonx.server.message.serialized_size"));
        Assert.Equal(actual.Length, sample.Value);
        Assert.Empty(sample.Tags);
    }

    [Fact]
    public void ProtocolPreservesParsing()
    {
        // Arrange
        var options = Options.Create(new JsonHubProtocolOptions());
        var protocol = new MetricsJsonHubProtocol(options);
        var input = new ReadOnlySequence<byte>(protocol.GetMessageBytes(PingMessage.Instance));

        // Act
        var parsed = protocol.TryParseMessage(ref input, Mock.Of<IInvocationBinder>(), out var message);

        // Assert
        Assert.True(parsed);
        Assert.IsType<PingMessage>(message);
        Assert.Equal("json", protocol.Name);
        Assert.True(input.IsEmpty);
        Assert.True(protocol.IsVersionSupported(protocol.Version));
    }

    [Fact]
    public async Task LeaseDurationsAreRecordedOnceAndWaitForExclusiveAccess()
    {
        // Arrange
        using var capture = new Capture();
        var repository = CreateRepository();
        var id = Guid.NewGuid();
        var owner = await repository.AcquireStateAsync(id);

        // Act
        var waiting = repository.AcquireStateAsync(id);
        Assert.False(waiting.IsCompleted);
        await owner.DisposeAsync();
        await owner.DisposeAsync();
        var next = await waiting;
        await next.DisposeAsync();

        // Assert
        Assert.Equal(2, capture.Read("gammonx.server.lease.wait").Length);
        Assert.Equal(2, capture.Read("gammonx.server.lease.hold").Length);
        Assert.All(capture.Read("gammonx.server.lease.wait"), sample => Assert.Empty(sample.Tags));
    }

    [Fact]
    public async Task FilterRecordsFailedCommandsWithoutArgumentLabels()
    {
        // Arrange
        using var capture = new Capture();
        var method = typeof(TestHub).GetMethod(nameof(TestHub.Command))!;
        using var services = new ServiceCollection().BuildServiceProvider();
        var context = new HubInvocationContext(Mock.Of<HubCallerContext>(), services, new TestHub(), method, ["private-player-id"]);
        var filter = new MetricsHubFilter();

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await filter.InvokeMethodAsync(context, _ => throw new InvalidOperationException("test")));

        // Assert
        var sample = Assert.Single(capture.Read("gammonx.server.command.duration"));
        Assert.Equal("Command", sample.Tags["operation"]);
        Assert.Single(sample.Tags);
    }

    [Fact]
    public async Task LateListenerSeesCurrentCountsAndRemovalReturnsToZero()
    {
        // Arrange
        var repository = CreateRepository();
        var connections = new PlayerConnectionRepository();
        var timers = new CancellationTokenServiceImpl();
        var bot = new BotMatchmakingService(connections);
        using var state = new ServerStateMetrics(() => repository.Count, () => timers.Count, () => bot.LobbyCount);
        var key = new QueueKey(MatchVariant.Backgammon, MatchModus.Bot, MatchType.FivePointGame, BotLevel.Easy);
        var entry = await bot.JoinQueueAsync(Guid.NewGuid(), key);
        Assert.True(bot.TryFindMatchLobby(entry.Id, out var lobby));
        await using var lease = await repository.AcquireStateAsync(lobby!.MatchId);
        lease.Create(key);
        var tokenKey = new CancellationTokenKey { MatchId = lobby.MatchId, PlayerId = lobby.Player1.Id, Category = CancellationTokenCategory.Turn };
        using var source = new CancellationTokenSource();
        timers.Store(tokenKey, source);

        // Act: attach only after the state exists; aliases must not inflate the lobby count.
        using var capture = new Capture();
        capture.Poll();

        // Assert
        Assert.Equal(1, capture.Read("gammonx.server.matches.active").Last().Value);
        Assert.Equal(1, capture.Read("gammonx.server.lobbies.pending").Last().Value);
        Assert.Equal(1, capture.Read("gammonx.server.deadlines.registered").Last().Value);

        // Act
        source.Cancel();
        lease.TryRemove();
        Assert.True(bot.TryRemoveMatchLobby(lobby.MatchId));
        Assert.False(bot.TryRemoveMatchLobby(lobby.MatchId));
        capture.Poll();

        // Assert
        Assert.Equal(0, capture.Read("gammonx.server.matches.active").Last().Value);
        Assert.Equal(0, capture.Read("gammonx.server.lobbies.pending").Last().Value);
        Assert.Equal(0, capture.Read("gammonx.server.deadlines.registered").Last().Value);
    }

    [Fact]
    public void GenerationReportsResultCountWithoutPositionLabels()
    {
        // Arrange
        using var capture = new Capture();
        var game = new GameSessionFactory(new DiceServiceFactory()).Create(Guid.NewGuid(), GameModus.Backgammon);

        // Act
        game.SetDiceRolls([1, 1], true);

        // Assert
        var sample = Assert.Single(capture.Read("gammonx.server.moves.sequences"));
        Assert.Equal(245, sample.Value);
        Assert.Empty(sample.Tags);
        Assert.Single(capture.Read("gammonx.server.moves.duration"));
    }

    private static MatchSessionRepository CreateRepository() => new(new MatchSessionFactory(new GameSessionFactory(new DiceServiceFactory())));

    public class TestHub : Hub
    {
        public void Command(string player) { }
    }

    public class CountingPayload
    {
        [JsonIgnore]
        public int Reads { get; private set; }
        public DateTime Timestamp => new(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);
        public string Text
        {
            get
            {
                Reads++;
                return "棋 ä " + new string('x', 10000);
            }
        }
    }

    private sealed class Capture : IDisposable
    {
        private readonly MeterListener _listener = new();
        private readonly List<Measurement> _measurements = new();
        private readonly Lock _gate = new();

        internal Capture()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == ServerMetrics.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Add(instrument, value, tags));
            _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Add(instrument, value, tags));
            _listener.SetMeasurementEventCallback<int>((instrument, value, tags, _) => Add(instrument, value, tags));
            _listener.Start();
        }

        internal void Poll() => _listener.RecordObservableInstruments();

        internal Measurement[] Read(string name)
        {
            lock (_gate)
            {
                return _measurements.Where(item => item.Name == name).ToArray();
            }
        }

        private void Add(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            lock (_gate)
            {
                _measurements.Add(new Measurement(instrument.Name, value, tags.ToArray().ToDictionary()));
            }
        }

        public void Dispose() => _listener.Dispose();
    }

    private sealed record Measurement(string Name, double Value, Dictionary<string, object?> Tags);
}
