namespace GammonX.Server
{
	public class BotServiceOptions
	{
		/// <summary>
		/// Gets or sets the base url of the wildbg bot service.
		/// </summary>
		public string WILDBG { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the base url of the mars bot service.
		/// </summary>
		public string MARS { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timeout in seconds
        /// </summary>
        public int TIMEOUT_SECONDS { get; set; } = 10;

		public int MAX_RETRY_ATTEMPTS { get; set; } = 3;

        public int RETRY_BASE_DELAY_MILLISECONDS { get; set; } = 1000;
	}
}
