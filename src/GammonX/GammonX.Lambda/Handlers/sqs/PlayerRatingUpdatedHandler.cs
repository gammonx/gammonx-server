using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;

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
        public PlayerRatingUpdatedHandler() : base()
        {
            // pass
        }

        // <inheritdoc />
        [LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
        public async Task HandleAsync(SQSEvent @event, ILambdaContext context)
		{
			try
			{
                if (_repo == null)
                {
                    context.Logger.LogInformation($"Setting up DI services...");
                    var services = Startup.Configure();
                    _repo = services.GetRequiredService<IDynamoDbRepository>();
                }

                // we expect exactly to match records, one for the winner and one for the loser
                if (@event.Records.Count == 2)
                {
                    context.Logger.LogInformation($"Processing message with id '{@event.Records[0].MessageId}'");
                    context.Logger.LogInformation($"Processing message with id '{@event.Records[1].MessageId}'");
                    var matchRecords = @event.Records.Select(r => JsonConvert.DeserializeObject<MatchRecordContract>(r.Body));
                    var wonMatch = matchRecords.FirstOrDefault(mr => mr?.Result == Models.Enums.MatchResult.Won);
                    var lostMatch = matchRecords.FirstOrDefault(mr => mr?.Result == Models.Enums.MatchResult.Lost);

                    if (wonMatch == null || lostMatch == null)
                    {
                        context.Logger.LogWarning($"The '{LambdaFunctions.PlayerRatingUpdatedFunc}' expects two match records with a decisive result.");
                        return;
                    }

					var results = new List<(PlayerRatingItem, RatingPeriodItem)>();
                    foreach (var record in matchRecords)
					{
                        var result = await ProcessMessageAsync(record?.PlayerId, wonMatch, lostMatch);
                        results.Add(result);
                    }
                    // we persist the player ratings and their rating periods after both players have been processed
					foreach (var result in results)
					{
						// we persist the updated player rating
						await _repo.SaveAsync(result.Item1);
                        // we persist the rating period entry
                        await _repo.SaveAsync(result.Item1);
                    }

                    context.Logger.LogInformation($"Processed rating update for player with id '{wonMatch?.PlayerId}' after match '{wonMatch?.Id}'");
                    context.Logger.LogInformation($"Processed rating update for player with id '{lostMatch?.PlayerId}' after match '{lostMatch?.Id}'");
                }
                else
                {
                    throw new InvalidOperationException($"The '{LambdaFunctions.PlayerRatingUpdatedFunc}' expects exactly two match records.");
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

		private async Task<(PlayerRatingItem, RatingPeriodItem)> ProcessMessageAsync(Guid? playerId, MatchRecordContract? wonMatch, MatchRecordContract? lostMatch)
		{
            if (_repo == null)
                throw new NullReferenceException("db repo must not be null");

            ArgumentNullException.ThrowIfNull(wonMatch, nameof(wonMatch));
			ArgumentNullException.ThrowIfNull(lostMatch, nameof(lostMatch));
            ArgumentNullException.ThrowIfNull(playerId, nameof(playerId));

            var wonMatchHistory = wonMatch.ToMatchHistory();
			var wonParser = HistoryParserFactory.Create<IMatchHistoryParser>(wonMatchHistory.Format);
			var wonParsedHistory = wonParser.ParseMatch(wonMatchHistory.Data);
			var wonMatchItem = wonMatch.ToMatch(wonParsedHistory);

            var lostMatchHistory = lostMatch.ToMatchHistory();
            var lostParser = HistoryParserFactory.Create<IMatchHistoryParser>(lostMatchHistory.Format);
            var lostParsedHistory = lostParser.ParseMatch(lostMatchHistory.Data);
            var lostMatchItem = lostMatch.ToMatch(lostParsedHistory);

            var result = await _repo.CalculatePlayerRatingAsync(playerId.Value, wonMatchItem, lostMatchItem);
			return result;
		}
	}
}
