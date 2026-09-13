using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using Amazon.DynamoDBv2.Model;

using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;

using GammonX.Models.Contracts;
using GammonX.Models.Helpers;

using Microsoft.Extensions.DependencyInjection;

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
		/// Default constructor for container based lambda execution.
		/// This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
		/// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
		/// region the Lambda function is executed in.
		/// </summary>
		public PlayerStatsUpdatedHandler(IDynamoDbRepository repo) : base(repo)
		{
			// pass
		}

		/// <summary>
		/// Default constructor for .zip based lambda execution. We need to kick off the DI manually.
		/// </summary>
		public PlayerStatsUpdatedHandler()
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
			for (var index = 0; index < @event.Records.Count; index++)
			{
				var message = @event.Records[index];
				try
				{
					await ProcessMessageAsync(message, context);
				}
				catch (Exception ex)
				{
					context.Logger.LogError(ex, $"An error occurred while processing stats update. Message id: '{message.MessageId}'");
					failures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
					for (var remainingIndex = index + 1; remainingIndex < @event.Records.Count; remainingIndex++)
					{
						failures.Add(new SQSBatchResponse.BatchItemFailure
						{
							ItemIdentifier = @event.Records[remainingIndex].MessageId
						});
					}
                    // We stop the current stat processing for this batch in order to ensure the FIFO order
					break;
				}
			}

			return new SQSBatchResponse(failures);
		}

		private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
		{
			if (Repo == null)
				throw new NullReferenceException("db repo must not be null");

			context.Logger.LogInformation($"Processing message with id '{message.MessageId}'");

			var json = message.Body;
			var matchRecord = JsonConvert.DeserializeObject<MatchRecordContract>(json);

			if (matchRecord == null)
			{
				throw new InvalidOperationException($"Could not deserialize stats update message '{message.MessageId}'.");
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
			var queriedMatches = await Repo.GetItemsByGSIPKAsync<MatchItem>(playerId, matchGsiSk);
			var playerMatches = (queriedMatches is null ? Enumerable.Empty<MatchItem>() : queriedMatches)
				.Where(match => match is not null)
				.ToList();

			var sourceMatch = playerMatches.SingleOrDefault(match => match.Id == newMatchId && match.PlayerId == playerId);

			if (sourceMatch == null)
			{
				throw new InvalidOperationException( $"Persisted source match '{newMatchId}' for player '{playerId}' is not available for stats calculation.");
			}

			var playerStatsItem = PlayerStatsItemFactory.CreateItem(
				playerId,
				matchRecord.Variant,
				matchRecord.Type,
				matchRecord.Modus,
				playerMatches);

            // We set the EndedAt time stamp as the marker in order to identify if the stat update is stale or a duplicate.
			playerStatsItem.SourceMatchEndedAt = sourceMatch.EndedAt;

			if (Repo is not IDynamoDbTransactionWriter transactionWriter)
			{
				throw new InvalidOperationException("Stats persistence requires transaction support.");
			}

			try
			{
				await transactionWriter.TransactPutAsync([CreateStatsPutOperation(playerStatsItem)]);
			}
			catch (TransactionCanceledException)
			{
				if (await IsStaleOrDuplicateAsync(Repo, playerStatsItem))
				{
					context.Logger.LogInformation(
						$"Skipped stale or duplicate stat update for player with id '{playerId}' after match '{newMatchId}'");
					return;
				}

				throw;
			}

			context.Logger.LogInformation($"Processed stat update for player with id '{playerId}' after match '{newMatchId}'");
		}

		internal static DynamoDbPutOperation CreateStatsPutOperation(PlayerStatsItem stats)
		{
			if (!stats.SourceMatchEndedAt.HasValue)
			{
				throw new ArgumentException("A source match completion time is required.", nameof(stats));
			}

			return DynamoDbPutOperation.Create(
				stats,
				"attribute_not_exists(#sourceMatchEndedAt) OR #sourceMatchEndedAt < :sourceMatchEndedAt",
				new Dictionary<string, string> { { "#sourceMatchEndedAt", "SourceMatchEndedAt" } },
				new Dictionary<string, AttributeValue>
				{
					{ ":sourceMatchEndedAt", new AttributeValue { S = DateTimeHelper.FormatUtc(stats.SourceMatchEndedAt.Value) } }
				});
		}

		internal static async Task<bool> IsStaleOrDuplicateAsync(IDynamoDbRepository repo, PlayerStatsItem candidate)
		{
			var existingItems = repo is IDynamoDbConsistentReader consistentReader
				? await consistentReader.GetItemsConsistentlyAsync<PlayerStatsItem>(candidate.PlayerId, candidate.SK)
				: await repo.GetItemsAsync<PlayerStatsItem>(candidate.PlayerId, candidate.SK);

			var existing = existingItems.SingleOrDefault();

			return existing?.SourceMatchEndedAt >= candidate.SourceMatchEndedAt;
		}
	}
}
