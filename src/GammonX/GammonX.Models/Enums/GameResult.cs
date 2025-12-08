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
		/// Won a game where the opponent declined a double. Single win.
		/// </summary>
		DoubleDeclined = 3,
		/// <summary>
		/// Won a game where the opponent resigned the game.
		/// </summary>
		Resign = 4,
		/// <summary>
		/// Lost a game where the player resigned the game.
		/// </summary>
		LostResign = 94,
		/// <summary>
		/// Lost a game by a back gammon, Opponent points are trippled.
		/// </summary>
		LostBackgammon = 95,
		/// <summary>
		/// Lost a game by a gammon. Opponent points are doubled.
		/// </summary>
		LostGammon = 96,
		/// <summary>
		/// Lost a game where a double was declined.
		/// </summary>
		LostDoubleDeclined = 97,
		/// <summary>
		/// Lost a game. No points awarded.
		/// </summary>
		LostSingle = 98,
		/// <summary>
		/// Game did not finish.
		/// </summary>
		Unknown = 99,
		/// <summary>
		/// Game concluded in a draw. (e.g. both mother checkers are pinned in plakoto)
		/// </summary>
		Draw = 100,
	}

	public static class GameResultExtensions
	{
		public static bool? HasWon(this GameResult gameResult)
		{
			switch (gameResult)
			{
				case GameResult.Gammon:
				case GameResult.Backgammon:
				case GameResult.Single:
				case GameResult.DoubleDeclined:
				case GameResult.Resign:
					return true;
				case GameResult.LostDoubleDeclined:
				case GameResult.LostSingle:
				case GameResult.LostResign:
				case GameResult.LostBackgammon:
				case GameResult.LostGammon:
					return false;
				case GameResult.Unknown:
				case GameResult.Draw:
				default:
					return null;
			}
		}
	}
}
