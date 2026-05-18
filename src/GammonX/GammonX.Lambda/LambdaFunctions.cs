using GammonX.Models.Enums;

namespace GammonX.Lambda
{
	internal static class LambdaFunctions
	{
        #region SQS Lambda Handlers

        public static string MatchCompletedFunc = WorkQueueType.MatchCompleted.GetName();

		public static string GameCompletedFunc = WorkQueueType.GameCompleted.GetName();

        public static string PlayerRatingUpdatedFunc = WorkQueueType.RatingUpdated.GetName();

        public static string PlayerStatsUpdatedFunc = WorkQueueType.StatsUpdated.GetName();

		public static string PlayerCreatedFunc = WorkQueueType.PlayerCreated.GetName();

        #endregion SQS Lambda Handlers

        #region API Gateway Lambda Handlers

        public const string ApiGatewayHandlerFunc = "API_GATEWAY_HANDLER";

        #endregion
    }
}
