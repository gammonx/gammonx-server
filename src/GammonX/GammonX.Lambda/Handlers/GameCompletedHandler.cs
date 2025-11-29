using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

using GammonX.DynamoDb.Repository;

using GammonX.Lambda.Extensions;

using GammonX.Models.Contracts;
using GammonX.Models.History;

using Newtonsoft.Json;

namespace GammonX.Lambda.Handlers
{
	/// <summary>
	/// Handles <see cref="LambdaFunctions.GameCompletedFunc"/> event.
	/// Writes game details for winner and loser and the game history.
	/// </summary>
	public class GameCompletedHandler : LambdaHandlerBaseImpl, ISqsLambdaHandler
	{
		/// <summary>
		/// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
		/// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
		/// region the Lambda function is executed in.
		/// </summary>
		public GameCompletedHandler(IDynamoDbRepository repo) : base(repo)
		{
			// pass
		}

		// <inheritdoc />
		public async Task HandleAsync(SQSEvent evnt, ILambdaContext context)
		{
			foreach (var message in evnt.Records)
			{				
				await ProcessMessageAsync(message, context);
			}
		}

		private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
		{
			context.Logger.LogInformation($"Processing message with id '{message.MessageId}'");

			var json = message.Body;
			var gameRecord = JsonConvert.DeserializeObject<GameRecordContract>(json);

			if (gameRecord == null)
			{
				context.Logger.LogError($"An error occurred while deserializing body of '{message.MessageId}'");
				return;
			}

			context.Logger.LogInformation($"Processing completed game with id '{gameRecord.Id}' for player '{gameRecord.PlayerId}'");

			// create game history item
			var gameHistory = gameRecord.ToGameHistory();
			// parse game history and calculate some stats
			var parserFactory = HistoryParserFactory.Create(gameHistory.Format);
			var parsedGameHistory = parserFactory.Parse(gameHistory.Data);
			// create game item
			var gameItem = gameRecord.ToGame(parsedGameHistory);

			await _repo.SaveAsync(gameItem);
			// TODO: avoid writing history twice
			await _repo.SaveAsync(gameHistory);

			context.Logger.LogInformation($"Processed completed game with id '{gameRecord.Id}' for player '{gameRecord.PlayerId}'");
		}
	}
}
