namespace GammonX.Server.Queue
{
    public class WorkQueueOptions
    {
        /// <summary>
		/// Gets or sets the service url for the sqs client.
		/// </summary>
		public string SERVICEURL { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the region of the sqs client.
        /// </summary>
        public string REGION { get; set; } = string.Empty;

        public string AWS_ACCESS_KEY_ID { get; set; } = string.Empty;

        public string AWS_SECRET_ACCESS_KEY { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the game completed queue.
        /// </summary>
        public string GAME_COMPLETED_QUEUE_URL { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the match completed queue.
        /// </summary>
        public string MATCH_COMPLETED_QUEUE_URL { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the player created queue.
        /// </summary>
        public string PLAYER_CREATED_QUEUE_URL { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the player stats updated queue.
        /// </summary>
        public string STATS_UPDATED_QUEUE_URL { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the player rating updated queue.
        /// </summary>
        public string RATING_UPDATED_QUEUE_URL { get; set; } = string.Empty;
    }
}
