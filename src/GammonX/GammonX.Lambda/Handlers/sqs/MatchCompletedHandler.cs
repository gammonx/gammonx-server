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
	/// Handles <see cref="LambdaFunctions.MatchCompletedFunc"/> event.
	/// Writes match details for winner and loser and the game history.
	/// </summary>
	public class MatchCompletedHandler : LambdaHandlerBaseImpl, ISqsLambdaHandler
	{
		/// <summary>
		/// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
		/// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
		/// region the Lambda function is executed in.
		/// </summary>
		public MatchCompletedHandler(IDynamoDbRepository repo) : base(repo)
		{
			// pass
		}

		// <inheritdoc />
		public async Task HandleAsync(SQSEvent @event, ILambdaContext context)
		{
			try
			{
                foreach (var message in @event.Records)
                {
                    await ProcessMessageAsync(message, context);
                }
            }
            catch (Exception ex)
            {
                foreach (var record in @event.Records)
                {
                    context.Logger.LogError(ex, $"An error occurred while processing rating update. Message id: '{record.MessageId}'");

                }
            }
        }

		private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
		{
			context.Logger.LogInformation($"Processing message with id '{message.MessageId}'");

			var json = message.Body;
			var matchRecord = JsonConvert.DeserializeObject<MatchRecordContract>(json);

			if (matchRecord == null)
			{
				context.Logger.LogError($"An error occurred while deserializing body of '{message.MessageId}'");
				return;
			}

			context.Logger.LogInformation($"Processing completed match with id '{matchRecord.Id}' for player '{matchRecord.PlayerId}'");

			// create match history item
			var matchHistory = matchRecord.ToMatchHistory();
			// parse match history and calculate some stats
			var parserFactory = HistoryParserFactory.Create<IMatchHistoryParser>(matchHistory.Format);
			var parsedHistory = parserFactory.ParseMatch(matchHistory.Data);
			// create match item
			var matchItem = matchRecord.ToMatch(parsedHistory);

			await _repo.SaveAsync(matchItem);
			// TODO: avoid writing history twice
			await _repo.SaveAsync(matchHistory);

			context.Logger.LogInformation($"Processed completed match with id '{matchRecord.Id}' for player '{matchRecord.PlayerId}'");
		}
	}
}
