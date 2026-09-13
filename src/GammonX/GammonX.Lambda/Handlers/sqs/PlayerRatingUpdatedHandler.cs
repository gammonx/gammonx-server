using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Amazon.DynamoDBv2.Model;

using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;
using GammonX.DynamoDb.Services;

using GammonX.Lambda.Extensions;

using GammonX.Models.Contracts;
using GammonX.Models.History;

using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

namespace GammonX.Lambda.Handlers
{
	/// <summary>
	/// Handles <see cref="LambdaFunctions.PlayerRatingUpdatedFunc"/> event.
	/// Calculates the updated rating for a given player.
	/// </summary>
	public class PlayerRatingUpdatedHandler : LambdaHandlerBaseImpl, ISqsLambdaHandler
	{
        /// <summary>
        /// Default constructor for container based lambda execution. 
        /// This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
		public PlayerRatingUpdatedHandler(IDynamoDbRepository repo) : base(repo)
		{
			// pass
		}

        /// <summary>
        /// Default constructor for .zip based lambda execution. We need to kick off the DI manually.
        /// </summary>
        public PlayerRatingUpdatedHandler()
        {
            // pass
        }

        // <inheritdoc />
        [LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
        public async Task HandleAsync(SQSEvent @event, ILambdaContext context)
		{
			try
			{
                if (Repo == null)
                {
                    context.Logger.LogInformation("Setting up DI services...");
                    var services = Startup.Configure();
                    Repo = services.GetRequiredService<IDynamoDbRepository>();
                }

                foreach (var message in @event.Records)
                {
                    context.Logger.LogInformation($"Processing message with id '{message.MessageId}'");
                    var work = JsonConvert.DeserializeObject<RatingUpdateWorkContract>(message.Body);
                    if (work == null)
                    {
                        throw new InvalidOperationException($"Unable to deserialize rating update message '{message.MessageId}'.");
                    }

                    var (winner, loser) = work.GetValidatedRecords();
                    var updated = await ProcessMessageAsync(winner, loser);
                    var action = updated ? "Processed" : "Skipped duplicate";
                    context.Logger.LogInformation($"{action} rating update for players '{winner.PlayerId}' and '{loser.PlayerId}' after match '{winner.Id}'");
                }
            }
			catch (Exception ex) 
			{
				foreach (var record in @event.Records)
				{
					context.Logger.LogError(ex, $"An error occurred while processing rating update. Message id: '{record.MessageId}'");

                }

                throw;
			}
		}

        private async Task<bool> ProcessMessageAsync(MatchRecordContract wonMatch, MatchRecordContract lostMatch)
		{
            if (Repo == null)
            {
                throw new NullReferenceException("db repo must not be null");
            }

            var periodFactory = ItemFactoryCreator.Create<RatingPeriodItem>();
            var periodSk = string.Format(periodFactory.SKFormat, wonMatch.Variant, wonMatch.Type, wonMatch.Modus, wonMatch.Id);
            var winnerPeriods = await Repo.GetItemsAsync<RatingPeriodItem>(wonMatch.PlayerId, periodSk);
            var loserPeriods = await Repo.GetItemsAsync<RatingPeriodItem>(lostMatch.PlayerId, periodSk);
            var winnerPeriodExists = winnerPeriods.Any();
            var loserPeriodExists = loserPeriods.Any();

            if (winnerPeriodExists && loserPeriodExists)
            {
                return false;
            }

            if (winnerPeriodExists || loserPeriodExists)
            {
                throw new InvalidOperationException($"Rating period state for match '{wonMatch.Id}' is inconsistent.");
            }

            var wonMatchHistory = wonMatch.ToMatchHistory();
			var wonParser = HistoryParserFactory.Create<IMatchHistoryParser>(wonMatchHistory.Format);
			var wonParsedHistory = wonParser.ParseMatch(wonMatchHistory.Data);
			var wonMatchItem = wonMatch.ToMatch(wonParsedHistory);
            var lostMatchItem = lostMatch.ToMatch(wonParsedHistory);

            var winnerResult = await Repo.CalculatePlayerRatingAsync(wonMatch.PlayerId, wonMatchItem, lostMatchItem);
            var loserResult = await Repo.CalculatePlayerRatingAsync(lostMatch.PlayerId, wonMatchItem, lostMatchItem);

            if (Repo is not IDynamoDbTransactionWriter transactionWriter)
            {
                throw new InvalidOperationException("Rating persistence requires transaction support.");
            }

            try
            {
                await transactionWriter.TransactPutAsync(
                [
                    DynamoDbPutOperation.Create(winnerResult.Item1),
                    DynamoDbPutOperation.Create(winnerResult.Item2, "attribute_not_exists(PK)"),
                    DynamoDbPutOperation.Create(loserResult.Item1),
                    DynamoDbPutOperation.Create(loserResult.Item2, "attribute_not_exists(PK)")
                ]);
                
                return true;
            }
            catch (TransactionCanceledException)
            {
                if (await IsDuplicateTransactionAsync(Repo, wonMatch.PlayerId, lostMatch.PlayerId, periodSk))
                    return false;

                throw;
            }
		}

        internal static async Task<bool> IsDuplicateTransactionAsync(
            IDynamoDbRepository repo,
            Guid winnerId,
            Guid loserId,
            string periodSk)
        {
            var winnerPeriods = await repo.GetItemsAsync<RatingPeriodItem>(winnerId, periodSk);
            var loserPeriods = await repo.GetItemsAsync<RatingPeriodItem>(loserId, periodSk);
            return winnerPeriods.Any() && loserPeriods.Any();
        }
	}
}
