using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using GammonX.Lambda.Services;
using GammonX.Models.Contracts;
using GammonX.Models.History;

using Newtonsoft.Json;

using Xunit;

namespace GammonX.Lambda.Tests.Sqs
{
	public class PlayerStatsUpdatedLambdaHandlerTests
	{
		[Fact]
		public async Task OnPlayerStatsUpdatedEventTest()
		{
			var parser = HistoryParserFactory.Create<IGameHistoryParser>(Models.Enums.HistoryFormat.MAT);

			// player ids must match with the game history
			var player1Id = Guid.Parse("cf0ab132-2279-43d3-911f-ed139ce5e7ba");
			var player2Id = Guid.Parse("e51f307e-3bf6-4408-b4b7-5fabd41b57b8");

			// GAME 1 :: PORTES
			var portesGameId = Guid.Parse("c57e0961-02e7-4aac-857f-565e9d78db09");
			var portesPath = Path.Combine("Data", "PortesGameHistory.txt");
			var portesGameHistory = File.ReadAllText(portesPath);
			var parsedPortesHistory = parser.ParseGame(portesGameHistory);
			var wonPortesGame = new GameRecordContract()
			{
				Id = portesGameId,
				PlayerId = player1Id,
				Result = Models.Enums.GameResult.Gammon,
				DoublingCubeValue = null,
				PipesLeft = 0,
				Format = Models.Enums.HistoryFormat.MAT,
				GameHistory = portesGameHistory
			};
			var lostPortesGame = new GameRecordContract()
			{
				Id = portesGameId,
				PlayerId = player2Id,
				Result = Models.Enums.GameResult.Lost,
				DoublingCubeValue = null,
				PipesLeft = 55,
				Format = Models.Enums.HistoryFormat.MAT,
				GameHistory = portesGameHistory
			};
			// GAME 2 :: PLAKOTO
			var plakotoGameId = Guid.Parse("3cf7ebbe-e0dd-4a2d-baa8-361014efa989");
			var plakotoPath = Path.Combine("Data", "PlakotoGameHistory.txt");
			var plakotoGameHistory = File.ReadAllText(plakotoPath);
			var parsedPlakotoHistory = parser.ParseGame(plakotoGameHistory);
			var wonPlakotoGame = new GameRecordContract()
			{
				Id = plakotoGameId,
				PlayerId = player1Id,
				Result = Models.Enums.GameResult.Single,
				DoublingCubeValue = null,
				PipesLeft = 0,
				Format = Models.Enums.HistoryFormat.MAT,
				GameHistory = plakotoGameHistory
			};
			var lostPlakotoGame = new GameRecordContract()
			{
				Id = plakotoGameId,
				PlayerId = player2Id,
				Result = Models.Enums.GameResult.Lost,
				DoublingCubeValue = null,
				PipesLeft = 55,
				Format = Models.Enums.HistoryFormat.MAT,
				GameHistory = plakotoGameHistory
			};
			// GAME 3 :: FEVGA
			var fevgaGameId = Guid.Parse("48fb1a93-9c2b-4245-803b-8361be6c6838");
			var fevgaPath = Path.Combine("Data", "FevgaGameHistory.txt");
			var fevgaGameHistory = File.ReadAllText(fevgaPath);
			var parsedFevgaHistory = parser.ParseGame(fevgaGameHistory);
			var wonFevgaGame = new GameRecordContract()
			{
				Id = fevgaGameId,
				PlayerId = player1Id,
				Result = Models.Enums.GameResult.Single,
				DoublingCubeValue = null,
				PipesLeft = 0,
				Format = Models.Enums.HistoryFormat.MAT,
				GameHistory = fevgaGameHistory
			};
			var lostFevgaGame = new GameRecordContract()
			{
				Id = fevgaGameId,
				PlayerId = player2Id,
				Result = Models.Enums.GameResult.Lost,
				DoublingCubeValue = null,
				PipesLeft = 55,
				Format = Models.Enums.HistoryFormat.MAT,
				GameHistory = fevgaGameHistory
			};


			// MATCH 1 :: TAVLI
			var matchId = Guid.Parse("888a356e-e09f-4a0f-b909-581f1ffb167e");
			var path = Path.Combine("Data", "TavliMatchHistory.txt");
			var matchHistory = File.ReadAllText(path);
			var wonTavliMatch = new MatchRecordContract()
			{
				Id = matchId,
				PlayerId = player1Id,
				Result = Models.Enums.MatchResult.Won,
				Variant = Models.Enums.MatchVariant.Tavli,
				Modus = Models.Enums.MatchModus.Normal,
				Type = Models.Enums.MatchType.CashGame,
				Format = Models.Enums.HistoryFormat.MAT,
				MatchHistory = matchHistory,
				Games = new[] { wonPortesGame, wonPlakotoGame, wonFevgaGame }
			};
			var lostTavliMatch = new MatchRecordContract()
			{
				Id = matchId,
				PlayerId = player2Id,
				Result = Models.Enums.MatchResult.Lost,
				Variant = Models.Enums.MatchVariant.Tavli,
				Modus = Models.Enums.MatchModus.Normal,
				Type = Models.Enums.MatchType.CashGame,
				Format = Models.Enums.HistoryFormat.MAT,
				MatchHistory = matchHistory,
				Games = new[] { lostPortesGame, lostPlakotoGame, lostFevgaGame }
			};

			var messageId1 = Guid.NewGuid().ToString();
			var messageId2 = Guid.NewGuid().ToString();

			var sqsEvent = new SQSEvent
			{
				Records = new List<SQSEvent.SQSMessage>
				{
					new SQSEvent.SQSMessage
					{
						Body = JsonConvert.SerializeObject(wonTavliMatch),
						MessageId = messageId1,
					},
					new SQSEvent.SQSMessage
					{
						Body = JsonConvert.SerializeObject(lostTavliMatch),
						MessageId = messageId2,
					}
				}
			};

			var logger = new TestLambdaLogger();
			var context = new TestLambdaContext
			{
				Logger = logger
			};

			var services = Startup.Configure();
            await Startup.ConfigureDynamoDbTableAsync(services);
            var handler = LambdaFunctionFactory.CreateSqsHandler(services, LambdaFunctions.PlayerStatsUpdatedFunc);

			await handler.HandleAsync(sqsEvent, context);
			Assert.Contains($"Processing message with id '{messageId1}'", logger.Buffer.ToString());
			Assert.Contains($"Processing message with id '{messageId2}'", logger.Buffer.ToString());
		}
	}
}
