using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models.Enums;

using GammonX.Server.Bot;
using GammonX.Server.Models;
using GammonX.Server.Services;

using GammonX.Server.Tests.Utils;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Tests
{
    public class MarsBotTests
    {
        private readonly HttpClient _wildBgClient = new() { BaseAddress = new Uri("http://localhost:8082/bot/wildbg/") };
        private readonly HttpClient _marsClient = new() { BaseAddress = new Uri("http://localhost:8083/bot/mars/") };

        [Theory]
        [InlineData(MatchVariant.Tavli, GameModus.Portes, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavli, GameModus.Portes, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavli, GameModus.Portes, MatchType.SevenPointGame)]
        public async Task TwoMarsBotsCanPlayStandalone(MatchVariant variant, GameModus activeModus, MatchType type)
        {
            var diceFactory = new DiceServiceFactory();
            var gameSessionFactory = new GameSessionFactory(diceFactory);
            var matchFactory = new MatchSessionFactory(gameSessionFactory);
            var matchSession = SessionUtils.CreateMatchSessionWithTwoBots(variant, type, matchFactory);
            Assert.Equal(Guid.Empty.ToString(), matchSession.Player1.ConnectionId);
            Assert.Equal(Guid.Empty.ToString(), matchSession.Player2.ConnectionId);
            Assert.True(matchSession.Player1.IsBot);
            Assert.True(matchSession.Player2.IsBot);

            var marsBotService = new MarsBotService(_marsClient);
            var wildBgBotService = new WildbgBotService(_wildBgClient);

            matchSession.Player1.AcceptNextGame();
            matchSession.Player2.AcceptNextGame();

            var botPlayer1Id = matchSession.Player1.Id;
            var botPlayer2Id = matchSession.Player2.Id;
            var activePlayerId = botPlayer1Id;
            var otherPlayerId = botPlayer2Id;

            Assert.Equal(activeModus, matchSession.GetGameModus());
            matchSession.Player1.AcceptNextGame();
            matchSession.Player2.AcceptNextGame();
            matchSession.StartMatch(activePlayerId);

            var gameSession = matchSession.GetGameSession(matchSession.GameRound);
            Assert.NotNull(gameSession);
            do
            {
                Assert.NotNull(gameSession);
                if (gameSession.Phase == GamePhase.WaitingForRoll)
                {
                    matchSession.RollDices(activePlayerId);
                }

                MoveSequenceModel? nextMoves;
                if (activeModus == GameModus.Plakoto || activeModus == GameModus.Fevga)
                {
                    nextMoves = await marsBotService.GetNextMovesAsync(matchSession, activePlayerId);
                }
                else
                {
                    nextMoves = await wildBgBotService.GetNextMovesAsync(matchSession, activePlayerId);
                }

                var hasWon = false;
                foreach (var nextMove in nextMoves.Moves)
                {
                    hasWon = matchSession.MoveCheckers(activePlayerId, nextMove.From, nextMove.To);
                    if (hasWon)
                        break;
                }

                if (!hasWon)
                {
                    matchSession.EndTurn(activePlayerId);
                    activePlayerId = otherPlayerId;
                    otherPlayerId = activePlayerId == botPlayer1Id ? botPlayer2Id : botPlayer1Id;
                }
                else if (!matchSession.IsMatchOver())
                {
                    matchSession.Player1.AcceptNextGame();
                    matchSession.Player2.AcceptNextGame();
                    matchSession.StartNextGame(activePlayerId);
                    gameSession = matchSession.GetGameSession(matchSession.GameRound);
                    Assert.NotNull(gameSession);
                    activeModus = gameSession.Modus;
                }
            }
            while (!matchSession.IsMatchOver());

            Assert.True(matchSession.IsMatchOver());
            Assert.False(matchSession.CanStartNextGame());
            gameSession = matchSession.GetGameSession(matchSession.GameRound);
            Assert.NotNull(gameSession);
            Assert.Equal(GamePhase.GameOver, gameSession.Phase);
            Assert.True(matchSession.Player1.Points > 0 || matchSession.Player2.Points > 0);
        }
    }
}
