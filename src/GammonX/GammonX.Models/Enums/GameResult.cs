namespace GammonX.Models.Enums
{
	public enum GameResult
	{
		/// <summary>
		/// Won a game with single point awarded.
		/// </summary>
		Single = 0,
		/// <summary>
		/// Won a game with a gammon. Points are doubled.
		/// </summary>
		Gammon = 1,
		/// <summary>
		/// Won a game with a backgammon. Points are trippled.
		/// </summary>
		Backgammon = 2,
		/// <summary>
		/// Lost a game where a double was declined.
		/// </summary>
		DoubleDeclined = 97,
		/// <summary>
		/// Lost a game. No points awarded.
		/// </summary>
		Lost = 98,
		/// <summary>
		/// Game did not finish.
		/// </summary>
		Unknown = 99
	}

	public static class GameResultExtensions
	{
		public static bool HasWon(this GameResult gameResult)
		{
			switch (gameResult)
			{
				case GameResult.Gammon:
				case GameResult.Backgammon:
				case GameResult.Single:
					return true;
				case GameResult.DoubleDeclined:
				case GameResult.Lost:
					return false;
				case GameResult.Unknown:
				default:
					throw new InvalidOperationException($"Unable to determine the game result for '{gameResult}'");
			}
		}
	}
}
