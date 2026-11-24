using GammonX.Engine.Services;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Bot;
using GammonX.Server.Contracts;
using GammonX.Server.Models;
using GammonX.Server.Services;
using GammonX.Server.Tests.Stubs;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Newtonsoft.Json;

using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Threading.Channels;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Tests.Integration
{
    public class SimpleBotGameIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public SimpleBotGameIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IGameSessionFactory>();
                    services.AddSingleton<IGameSessionFactory>(new TavliStartGameSessionFactoryStub());
                    services.RemoveAll<IDiceServiceFactory>();
                    services.AddSingleton<IDiceServiceFactory>(new StartDiceServiceFactoryStub());
                    services.RemoveAll<IBotService>();
                    services.AddKeyedSingleton<IBotService>(WellKnownBotServices.Mars, new SimpleBotService());
                    services.AddKeyedSingleton<IBotService>(WellKnownBotServices.WildBg, new SimpleBotService());
                });
            });
        }

        [Fact]
        public async Task StartSimpleBotMatch()
        {
            // Arrange.
            using var client = _factory.CreateClient();
            var serverUri = client.BaseAddress!.ToString().TrimEnd('/');
            var cancellationToken = TestContext.Current.CancellationToken;
            var player = new JoinRequest(Guid.NewGuid(), MatchVariant.Tavli, MatchModus.Bot, MatchType.CashGame, BotLevel.Hard);
            var botId = WellKnownBotPlayers.GetBotPlayerId(BotLevel.Hard);

            using var response = await client.PostAsJsonAsync("/game/api/matches/join", player, cancellationToken);
            response.EnsureSuccessStatusCode();
            var resultJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var joinResponse = JsonConvert.DeserializeObject<ResponseContract<RequestQueueEntryPayload>>(resultJson);
            var joinPayload = Assert.IsType<RequestQueueEntryPayload>(joinResponse?.Payload);

            // Bot matchmaking creates a lobby immediately; there is no queue to poll.
            Assert.Equal(QueueEntryStatus.OpponentFound, joinPayload.Status);
            Assert.Null(joinPayload.QueueId);
            Assert.NotNull(joinPayload.MatchId);
            var matchId = joinPayload.MatchId.Value;

            await using var connection = new HubConnectionBuilder()
                .WithUrl($"{serverUri}/matchhub", options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                })
                .Build();

            var unexpectedEvents = new ConcurrentQueue<string>();
            foreach (var eventName in new[]
            {
                ServerEventTypes.ErrorEvent,
                ServerEventTypes.MatchLobbyWaitingEvent,
                ServerEventTypes.GameWaitingEvent,
                ServerEventTypes.MatchWaitingEvent
            })
            {
                connection.On<object>(eventName, _ => unexpectedEvents.Enqueue(eventName));
            }

            var lobbyFound = Subscribe(connection, ServerEventTypes.MatchLobbyFoundEvent);
            var waitingForStart = Subscribe(connection, ServerEventTypes.MatchWaitingForStartEvent);
            var matchStarted = Subscribe(connection, ServerEventTypes.MatchStartedEvent);
            var gameStarted = Subscribe(connection, ServerEventTypes.GameStartedEvent);
            var gameStates = Subscribe(connection, ServerEventTypes.GameStateEvent);

            // Act: join and start the match, then play a complete human turn.
            await connection.StartAsync(cancellationToken);
            await connection.InvokeAsync(ServerCommands.JoinMatchCommand, matchId.ToString(), player.PlayerId.ToString(), cancellationToken);

            var lobby = await ReadPayloadAsync<EventMatchLobbyPayload>(lobbyFound);
            Assert.True(lobby.MatchFound);
            Assert.Equal(matchId, lobby.Id);
            Assert.Equal(player.PlayerId, lobby.Player1);
            Assert.Equal(botId, lobby.Player2);
            Assert.Empty(lobby.AllowedCommands);

            var waiting = await ReadPayloadAsync<EventMatchStatePayload>(waitingForStart);
            AssertMatchState(waiting, matchId, player.PlayerId, botId);
            Assert.Equal(3, waiting.AllowedCommands.Length);
            Assert.Contains(ServerCommands.StartMatchCommand, waiting.AllowedCommands);
            Assert.Null(waiting.Player1!.StartDiceRoll);
            Assert.Null(waiting.Player2!.StartDiceRoll);

            await connection.InvokeAsync(ServerCommands.StartMatchCommand, matchId.ToString(), cancellationToken);

            var started = await ReadPayloadAsync<EventMatchStatePayload>(matchStarted);
            AssertMatchState(started, matchId, player.PlayerId, botId);
            Assert.Equal(3, started.AllowedCommands.Length);
            Assert.Contains(ServerCommands.MoveCommand, started.AllowedCommands);
            Assert.Equal(6, started.Player1!.StartDiceRoll);
            Assert.Equal(1, started.Player2!.StartDiceRoll);

            var game = await ReadPayloadAsync<EventGameStatePayload>(gameStarted);
            Assert.Equal(matchId, game.MatchId);
            Assert.Equal(player.PlayerId, game.ActiveTurn);
            Assert.Equal(1, game.TurnNumber);
            Assert.Equal(GamePhase.Rolling, game.Phase);
            Assert.NotEmpty(game.MoveSequences);

            // Each hint is one command. Read fresh hints after every move until the turn is complete.
            while (game.AllowedCommands.Contains(ServerCommands.MoveCommand))
            {
                Assert.NotEmpty(game.MoveSequences);
                Assert.All(game.MoveSequences, sequence => Assert.Single(sequence.Moves));
                var move = Assert.Single(game.MoveSequences.First().Moves);
                await connection.InvokeAsync(ServerCommands.MoveCommand, matchId.ToString(), move.From, move.To, cancellationToken);
                game = await ReadPayloadAsync<EventGameStatePayload>(gameStates);
                Assert.Equal(player.PlayerId, game.ActiveTurn);
            }
            Assert.Equal(GamePhase.WaitingForEndTurn, game.Phase);
            Assert.Empty(game.MoveSequences);
            Assert.Contains(ServerCommands.EndTurnCommand, game.AllowedCommands);

            await connection.InvokeAsync(ServerCommands.EndTurnCommand, matchId.ToString(), cancellationToken);

            // Assert: the bot completed its turn and handed control back to the human.
            do
            {
                game = await ReadPayloadAsync<EventGameStatePayload>(gameStates);
                Assert.Equal(matchId, game.MatchId);
            }
            while (game.ActiveTurn != player.PlayerId || game.TurnNumber != 3);

            Assert.Equal(GamePhase.WaitingForRoll, game.Phase);
            Assert.Contains(ServerCommands.RollCommand, game.AllowedCommands);

            await connection.InvokeAsync(ServerCommands.RollCommand, matchId.ToString(), cancellationToken);
            game = await ReadPayloadAsync<EventGameStatePayload>(gameStates);
            Assert.Equal(player.PlayerId, game.ActiveTurn);
            Assert.Equal(3, game.TurnNumber);
            Assert.Equal(GamePhase.Rolling, game.Phase);
            Assert.NotEmpty(game.MoveSequences);
            Assert.Empty(unexpectedEvents);
        }

        private static void AssertMatchState(EventMatchStatePayload payload, Guid matchId, Guid playerId, Guid botId)
        {
            Assert.Equal(matchId, payload.Id);
            Assert.Equal($"match_{matchId}", payload.GroupName);
            Assert.Equal(1, payload.GameRound);
            Assert.Equal(MatchModus.Bot, payload.Modus);
            Assert.Null(payload.Winner);
            Assert.Null(payload.WinnerPoints);
            Assert.Null(payload.Loser);
            Assert.Null(payload.LoserPoints);
            Assert.NotNull(payload.Player1);
            Assert.NotNull(payload.Player2);
            Assert.Equal(playerId, payload.Player1.Id);
            Assert.Equal(botId, payload.Player2.Id);
            Assert.Null(payload.Player1.UserName);
            Assert.Null(payload.Player2.UserName);
            Assert.Contains(ServerCommands.ResignGameCommand, payload.AllowedCommands);
            Assert.Contains(ServerCommands.ResignMatchCommand, payload.AllowedCommands);
        }

        private static ChannelReader<object> Subscribe(HubConnection connection, string eventName)
        {
            var channel = Channel.CreateUnbounded<object>();
            connection.On<object>(eventName, response =>
            {
                channel.Writer.TryWrite(response);
            });
            return channel.Reader;
        }

        private static async Task<T> ReadPayloadAsync<T>(ChannelReader<object> events) where T : EventPayloadBase
        {
            // Assert on the test task: SignalR catches exceptions thrown by event handlers.
            var response = await events.ReadAsync(TestContext.Current.CancellationToken).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
            var contract = JsonConvert.DeserializeObject<EventResponseContract<T>>(response.ToString() ?? "");
            Assert.NotNull(contract);
            return Assert.IsType<T>(contract.Payload);
        }
    }
}
