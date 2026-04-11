namespace GammonX.Server
{
	public class BotServiceOptions
	{
		/// <summary>
		/// Gets or sets the base url of the wildbg bot service.
		/// </summary>
		public string WildBg { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the base url of the mars bot service.
		/// </summary>
		public string Mars { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 10;
	}
}
