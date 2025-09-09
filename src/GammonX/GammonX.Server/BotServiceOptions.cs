namespace GammonX.Server
{
	public class BotServiceOptions
	{
		/// <summary>
		/// Gets or sets the base url of the configured bot service.
		/// </summary>
		public string BaseUrl { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the timeout in seconds
		/// </summary>
		public int TimeoutSeconds { get; set; } = 10;
	}
}
