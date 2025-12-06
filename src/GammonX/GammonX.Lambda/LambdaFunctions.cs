namespace GammonX.Lambda
{
	public static class LambdaFunctions
	{
        #region SQS Lambda Handlers

        public const string MatchCompletedFunc = "MATCH_COMPELTED";

		public const string GameCompletedFunc = "GAME_COMPLETED";

		public const string PlayerRatingUpdatedFunc = "PLAYER_RATING_UPDATED";

		public const string PlayerStatsUpdatedFunc = "PLAYER_STATS_UPDATED";

		public const string PlayerCreatedFunc = "PLAYER_CREATED";

        #endregion SQS Lambda Handlers

        #region API Gateway Lambda Handlers

        public const string ApiGatewayHandlerFunc = "API_GATEWAY_HANDLER";

        #endregion
    }
}
