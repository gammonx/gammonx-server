namespace GammonX.Models.Enums
{
	public enum MatchResult
	{
		Won = 1,
		Lost = 2,
		/// <summary>
		/// Match was not finished.
		/// </summary>
		Unknown = 99
	}

	public static class MatchResultExtensions
	{
		public static bool? HasWon(this MatchResult gameResult)
		{
			switch (gameResult)
			{
				case MatchResult.Won:
					return true;
				case MatchResult.Lost:
					return false;
				case MatchResult.Unknown:
				default:
					return null;
			}
		}
	}
}
