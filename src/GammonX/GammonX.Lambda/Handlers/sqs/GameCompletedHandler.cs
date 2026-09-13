using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;

using GammonX.DynamoDb.Repository;

using GammonX.Lambda.Extensions;

using GammonX.Models.Contracts;
using GammonX.Models.History;

using Microsoft.Extensions.DependencyInjection;

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
		/// Default constructor for container based lambda execution. 
		/// This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
		/// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
		/// region the Lambda function is executed in.
		/// </summary>
		public GameCompletedHandler(IDynamoDbRepository repo) : base(repo)
		{
			// pass
		}

		/// <summary>
		/// Default constructor for .zip based lambda execution. We need to kick off the DI manually.
		/// </summary>
		public GameCompletedHandler()
		{
			// pass
		}

        // <inheritdoc />
        [LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
		public async Task<SQSBatchResponse> HandleAsync(SQSEvent @event, ILambdaContext context)
		{
			if (Repo == null)
			{
				context.Logger.LogInformation("Setting up DI services...");
				var services = Startup.Configure();
				Repo = services.GetRequiredService<IDynamoDbRepository>();
			}

			var failures = new List<SQSBatchResponse.BatchItemFailure>();
			foreach (var message in @event.Records)
			{
				try
				{
					await ProcessMessageAsync(message, context);
				}
				catch (Exception ex)
				{
					context.Logger.LogError(ex, $"An error occurred while processing game completed. Message id: '{message.MessageId}'");
					failures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
				}
			}

			return new SQSBatchResponse(failures);
		}

		private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
		{
			if (Repo == null)
			{
                throw new NullReferenceException("db repo must not be null");
			}

			context.Logger.LogInformation($"Processing message with id '{message.MessageId}'");

			var json = message.Body;
			var work = JsonConvert.DeserializeObject<GameCompletedWorkContract>(json);

			if (work == null)
			{
				throw new InvalidOperationException($"Unable to deserialize game completed message '{message.MessageId}'.");
			}
            
			var (winner, loser) = work.GetValidatedRecords();

			context.Logger.LogInformation($"Processing completed game with id '{winner.Id}' for players '{winner.PlayerId}' and '{loser.PlayerId}'");

			var gameHistory = winner.ToGameHistory();
			var parserFactory = HistoryParserFactory.Create<IGameHistoryParser>(gameHistory.Format);
			var parsedHistory = parserFactory.ParseGame(gameHistory.Data);
			var winnerGame = winner.ToGame(parsedHistory);
			var loserGame = loser.ToGame(parsedHistory);

			if (Repo is not IDynamoDbTransactionWriter transactionWriter)
			{
				throw new InvalidOperationException("Completed game persistence requires transaction support.");
			}

			await transactionWriter.TransactPutAsync(
			[
				DynamoDbPutOperation.Create(winnerGame),
				DynamoDbPutOperation.Create(loserGame),
				DynamoDbPutOperation.Create(gameHistory)
			]);

			context.Logger.LogInformation($"Processed completed game with id '{winner.Id}' for players '{winner.PlayerId}' and '{loser.PlayerId}'");
		}
	}
}
