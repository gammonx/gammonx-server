using System.Net;
using System.Text.Json;

using GammonX.Engine.Services;

using GammonX.Models.Enums;

using GammonX.Server.Bot;
using GammonX.Server.Models;
using GammonX.Server.Services;

using GammonX.Server.Tests.Http;
using GammonX.Server.Tests.Utils;

using Microsoft.AspNetCore.Http;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Tests.Bot.Mars
{
    public class MarsBotServiceTests
    {
        [Fact]
        public async Task ExpertMoveFallsBackToHardOnlyAfterTimeout()
        {
            var handler = new ScriptedHttpMessageHandler(
                _ => Task.FromException<HttpResponseMessage>(new TaskCanceledException("Mars timed out")),
                _ => Task.FromResult(MarsStubs.JsonResponse(HttpStatusCode.OK, MarsStubs.MoveResponse)));
            using var client = MarsStubs.CreateClient(handler, 1);
            var botService = new MarsBotService(client);
            var (matchSession, botPlayerId) = CreateExpertSession();

            var result = await botService.GetNextMovesAsync(matchSession, botPlayerId, CancellationToken.None);

            Assert.Empty(result.Moves);
            Assert.Equal(2, handler.RequestCount);
            Assert.Equal((int)BotLevel.Expert, GetBotLevel(handler.RequestBodies[0]));
            Assert.Equal((int)BotLevel.Hard, GetBotLevel(handler.RequestBodies[1]));
        }

        [Fact]
        public async Task ExpertMoveDoesNotFallbackForPermanentHttpFailure()
        {
            var handler = new ScriptedHttpMessageHandler(
                _ => Task.FromResult(MarsStubs.JsonResponse(HttpStatusCode.InternalServerError, "Mars failed")));
            using var client = MarsStubs.CreateClient(handler, 1);
            var botService = new MarsBotService(client);
            var (matchSession, botPlayerId) = CreateExpertSession();

            var exception = await Assert.ThrowsAsync<BadHttpRequestException>(() =>
                botService.GetNextMovesAsync(matchSession, botPlayerId, CancellationToken.None));

            Assert.Contains("Mars failed", exception.Message);
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public async Task ExpertMoveDoesNotFallbackAfterCallerCancellation()
        {
            var handler = new ScriptedHttpMessageHandler(
                _ => Task.FromResult(MarsStubs.JsonResponse(HttpStatusCode.OK, MarsStubs.MoveResponse)));
            using var client = MarsStubs.CreateClient(handler, 1);
            var botService = new MarsBotService(client);
            var (matchSession, botPlayerId) = CreateExpertSession();
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                botService.GetNextMovesAsync(matchSession, botPlayerId, cancellationTokenSource.Token));

            Assert.Equal(0, handler.RequestCount);
        }

        private static int GetBotLevel(string requestBody)
        {
            using var document = JsonDocument.Parse(requestBody);
            return document.RootElement.GetProperty("botLevel").GetInt32();
        }

        private static (IMatchSessionModel MatchSession, Guid BotPlayerId) CreateExpertSession()
        {
            var diceFactory = new DiceServiceFactory();
            var gameSessionFactory = new GameSessionFactory(diceFactory);
            var matchSessionFactory = new MatchSessionFactory(gameSessionFactory);
            var matchSession = matchSessionFactory.Create(
                Guid.NewGuid(),
                new QueueKey(MatchVariant.Backgammon, MatchModus.Bot, MatchType.CashGame, BotLevel.Expert));

            var player = SessionUtils.CreatePlayerConnection();
            var botPlayer = new PlayerConnection(Guid.NewGuid());
            botPlayer.SetConnectionId(Guid.Empty.ToString());
            matchSession.JoinSession(player);
            matchSession.JoinSession(botPlayer);
            matchSession.Player1.AcceptNextGame();
            matchSession.Player2.AcceptNextGame();
            matchSession.StartMatch(botPlayer.Id);

            return (matchSession, botPlayer.Id);
        }
    }
}
