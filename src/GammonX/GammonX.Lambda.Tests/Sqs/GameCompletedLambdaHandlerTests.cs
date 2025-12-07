using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using GammonX.Lambda.Services;
using GammonX.Models.Contracts;

using Newtonsoft.Json;

using Xunit;

namespace GammonX.Lambda.Tests.Sqs
{
	public class GameCompletedLambdaHandlerTests
	{
		[Fact]
		public async Task OnGameCompletedEventTest()
		{
			var gameId = Guid.Parse("c57e0961-02e7-4aac-857f-565e9d78db09");
			// player ids must match with the game history
			var player1Id = Guid.Parse("cf0ab132-2279-43d3-911f-ed139ce5e7ba");
			var player2Id = Guid.Parse("e51f307e-3bf6-4408-b4b7-5fabd41b57b8");

			var path = Path.Combine("Data", "PortesGameHistory.txt");
			var gameHistory = File.ReadAllText(path);

			var wonGameRecord = new GameRecordContract()
			{
				Id = gameId,
				PlayerId = player1Id,
				Result = Models.Enums.GameResult.Single,
				DoublingCubeValue = null,
				PipesLeft = 0,
				Format = Models.Enums.HistoryFormat.MAT,
				GameHistory = gameHistory
			};
			var lostGameRecord = new GameRecordContract()
			{
				Id = gameId,
				PlayerId = player2Id,
				Result = Models.Enums.GameResult.LostSingle,
				DoublingCubeValue = null,
				PipesLeft = 55,
				Format = Models.Enums.HistoryFormat.MAT,
				GameHistory = gameHistory
			};

			var messageId1 = Guid.NewGuid().ToString();
			var messageId2 = Guid.NewGuid().ToString();

			var sqsEvent = new SQSEvent
			{
				Records = new List<SQSEvent.SQSMessage>
				{
					new SQSEvent.SQSMessage
					{
						Body = JsonConvert.SerializeObject(wonGameRecord),
						MessageId = messageId1,
					},
					new SQSEvent.SQSMessage
					{
						Body = JsonConvert.SerializeObject(lostGameRecord),
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
            var handler = LambdaFunctionFactory.CreateSqsHandler(services, LambdaFunctions.GameCompletedFunc);

			await handler.HandleAsync(sqsEvent, context);
            Assert.Contains($"Processing message with id '{messageId1}'", logger.Buffer.ToString());
            Assert.Contains($"Processing message with id '{messageId2}'", logger.Buffer.ToString());
            Assert.Contains($"Processed completed game with id '{gameId}'", logger.Buffer.ToString());
		}
	}
}
