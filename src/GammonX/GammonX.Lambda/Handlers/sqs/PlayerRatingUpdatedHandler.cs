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
        private const int MaxRatingUpdateAttempts = 3;

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
                catch (Exception ex)
                {
                    context.Logger.LogError(ex, $"An error occurred while processing rating update. Message id: '{message.MessageId}'");
                    failures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
                }
            }

            return new SQSBatchResponse(failures);
		}

        private async Task<bool> ProcessMessageAsync(MatchRecordContract wonMatch, MatchRecordContract lostMatch)
		{
            if (Repo == null)
            {
                throw new NullReferenceException("db repo must not be null");
            }

            var periodFactory = ItemFactoryCreator.Create<RatingPeriodItem>();
            var periodSk = string.Format(periodFactory.SKFormat, wonMatch.Variant, wonMatch.Type, wonMatch.Modus, wonMatch.Id);
            var wonMatchHistory = wonMatch.ToMatchHistory();
			var wonParser = HistoryParserFactory.Create<IMatchHistoryParser>(wonMatchHistory.Format);
			var wonParsedHistory = wonParser.ParseMatch(wonMatchHistory.Data);
			var wonMatchItem = wonMatch.ToMatch(wonParsedHistory);
            var lostMatchItem = lostMatch.ToMatch(wonParsedHistory);

            if (Repo is not IDynamoDbTransactionWriter transactionWriter)
            {
                throw new InvalidOperationException("Rating persistence requires transaction support.");
            }

            return await ExecuteRatingUpdateAsync(
                wonMatch.Id,
                () => GetRatingPeriodStateAsync(Repo, wonMatch.PlayerId, lostMatch.PlayerId, periodSk),
                async () =>
                {
                    var winnerResult = await Repo.CalculatePlayerRatingAsync(wonMatch.PlayerId, wonMatchItem, lostMatchItem);
                    var loserResult = await Repo.CalculatePlayerRatingAsync(lostMatch.PlayerId, wonMatchItem, lostMatchItem);
                    return
                    [
                        CreateRatingPutOperation(winnerResult.Item1),
                        DynamoDbPutOperation.Create(winnerResult.Item2, "attribute_not_exists(PK)"),
                        CreateRatingPutOperation(loserResult.Item1),
                        DynamoDbPutOperation.Create(loserResult.Item2, "attribute_not_exists(PK)")
                    ];
                },
                operations => transactionWriter.TransactPutAsync(operations));
		}

        internal static async Task<bool> ExecuteRatingUpdateAsync(
            Guid matchId,
            Func<Task<(bool WinnerExists, bool LoserExists)>> getPeriodStateAsync,
            Func<Task<DynamoDbPutOperation[]>> createOperationsAsync,
            Func<IEnumerable<DynamoDbPutOperation>, Task> writeAsync)
        {
            for (var attempt = 1; attempt <= MaxRatingUpdateAttempts; attempt++)
            {
                // We get the latest rating period
                var periodState = await getPeriodStateAsync();

                ValidatePeriodState(matchId, periodState);

                if (periodState.WinnerExists)
                {
                    return false;
                }

                // We calculate the rating updates for winner and loser and prepare the corresponding DynamoDB put operations.
                var operations = await createOperationsAsync();

                try
                {
                    // We put the results atomically and consistently into the db
                    await writeAsync(operations);
                    return true;
                }
                catch (TransactionCanceledException)
                {
                    // We retry until max is reached
                    periodState = await getPeriodStateAsync();
                    ValidatePeriodState(matchId, periodState);
                    if (periodState.WinnerExists)
                    {
                        return false;
                    }

                    if (attempt == MaxRatingUpdateAttempts)
                    {
                        throw;
                    }

                    await Task.Delay((1 << (attempt - 1)) * 25);
                }
            }

            throw new InvalidOperationException("Rating update retry loop completed unexpectedly.");
        }

        private static void ValidatePeriodState(Guid matchId, (bool WinnerExists, bool LoserExists) periodState)
        {
            if (periodState.WinnerExists != periodState.LoserExists)
            {
                throw new InvalidOperationException($"Rating period state for match '{matchId}' is inconsistent.");
            }
        }

        internal static DynamoDbPutOperation CreateRatingPutOperation(PlayerRatingItem rating)
        {
            return DynamoDbPutOperation.CreateVersioned(rating, rating.Revision - 1);
        }

        internal static async Task<bool> IsDuplicateTransactionAsync(
            IDynamoDbRepository repo,
            Guid winnerId,
            Guid loserId,
            string periodSk)
        {
            var state = await GetRatingPeriodStateAsync(repo, winnerId, loserId, periodSk);
            return state.WinnerExists && state.LoserExists;
        }

        private static async Task<(bool WinnerExists, bool LoserExists)> GetRatingPeriodStateAsync(
            IDynamoDbRepository repo,
            Guid winnerId,
            Guid loserId,
            string periodSk)
        {
            var winnerPeriods = await GetItemsAsync<RatingPeriodItem>(repo, winnerId, periodSk);
            var loserPeriods = await GetItemsAsync<RatingPeriodItem>(repo, loserId, periodSk);
            return (winnerPeriods.Any(), loserPeriods.Any());
        }

        private static Task<IEnumerable<T>> GetItemsAsync<T>(IDynamoDbRepository repo, Guid id, string sk)
        {
            return repo is IDynamoDbConsistentReader consistentReader
                ? consistentReader.GetItemsConsistentlyAsync<T>(id, sk)
                : repo.GetItemsAsync<T>(id, sk);
        }
	}
}
