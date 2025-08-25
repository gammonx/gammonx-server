namespace GammonX.Server.Models
{
	/// <summary>
	/// 
	/// </summary>
	public enum GamePhase
	{
		/// <summary>
		/// The active player is awaited to roll the dices.
		/// </summary>
		WaitingForRoll = 0,
		/// <summary>
		/// The active player has rolled his dices and is awaited to move his checkers.
		/// </summary>
		Rolling = 1,
		/// <summary>
		/// The active player has already moved his checkers but still has some dices left to use.
		/// </summary>
		Moving = 2,
		/// <summary>
		/// The other player is the active player. Waiting for the opponent to finish his turn.
		/// </summary>
		WaitingForOpponent = 3,
		/// <summary>
		/// The game round is finished, but the match session is still active.
		/// </summary>
		Finished = 4,
		/// <summary>
		/// The active player has used up all his dices and is awaited to end his turn.
		/// </summary>
		WaitingForEndTurn = 5,
		/// <summary>
		/// The active player has made his last move and the game is over.
		/// </summary>
		GameOver = 6,
		/// <summary>
		/// Default game phase as long as the game session is not started yet.
		/// </summary>
		NotStarted = 98,
		/// <summary>
		/// Unknown game phase, used for debugging purposes.
		/// </summary>
		Unknown = 99
	}
}
