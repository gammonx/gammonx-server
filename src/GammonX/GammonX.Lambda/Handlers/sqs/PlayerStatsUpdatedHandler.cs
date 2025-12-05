using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;

using GammonX.Lambda.Extensions;

using GammonX.Models.Contracts;
using GammonX.Models.History;

using Newtonsoft.Json;

namespace GammonX.Lambda.Handlers
{
	/// <summary>
	/// Handles <see cref="LambdaFunctions.PlayerStatsUpdatedFunc"/> event.
	/// Calculates the updated stats for a given player.
	/// </summary>
	public class PlayerStatsUpdatedHandler : LambdaHandlerBaseImpl, ISqsLambdaHandler
	{
		/// <summary>
		/// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
		/// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
		/// region the Lambda function is executed in.
		/// </summary>
		public PlayerStatsUpdatedHandler(IDynamoDbRepository repo) : base(repo)
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

			var playerId = matchRecord.PlayerId;
			var newMatchId = matchRecord.Id;

			context.Logger.LogInformation($"Processing stat update for player with id '{playerId}' after match '{newMatchId}'");

			// get all matches of the player for the given variant, type and modus
			var matchItemFactory = new MatchItemFactory();
			var variantStr = matchRecord.Variant.ToString();
			var typeStr = matchRecord.Type.ToString();
			var modusStr = matchRecord.Modus.ToString();
			var matchGsiSk = string.Format(matchItemFactory.GSI1SKAllFormat, variantStr, typeStr, modusStr);
			var playerMatches = (await _repo.GetItemsByGSIPKAsync<MatchItem>(newMatchId, matchGsiSk)).ToList();

			// we check if the finished match was already posted to the db
			if (!playerMatches.Any(pm => pm.Id.Equals(matchRecord.Id)))
			{
				var matchHistory = matchRecord.ToMatchHistory();
				var parserFactory = HistoryParserFactory.Create<IMatchHistoryParser>(matchHistory.Format);
				var parsedHistory = parserFactory.ParseMatch(matchHistory.Data);
				var matchItem = matchRecord.ToMatch(parsedHistory);
				playerMatches.Add(matchItem);
			}

			var playerStatsItem = PlayerStatsItemFactory.CreateItem(
				playerId,
				matchRecord.Variant,
				matchRecord.Type,
				matchRecord.Modus,
				playerMatches);

			await _repo.SaveAsync(playerStatsItem);

			context.Logger.LogInformation($"Processed stat update for player with id '{playerId}' after match '{newMatchId}'");
		}
	}
}
