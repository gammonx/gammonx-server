namespace GammonX.Server
{
	public static class ServerCommands
	{
		/// <summary>
		/// The caller can join a match session with an existing match id created by the matchmaking service.
		/// </summary>
		public static readonly string JoinMatchCommand = "JoinMatch";

		/// <summary>
		/// The caller can start a game round if successfuly joined a match session.
		/// </summary>
		public static readonly string StartGameCommand = "StartGame";

		/// <summary>
		/// If the active player, the caller can roll the dices and start his turn.
		/// </summary>
		public static readonly string RollCommand = "Roll";

		/// <summary>
		/// If the active player, the caller can move his checkers from one position to another.
		/// </summary>
		public static readonly string MoveCommand = "Move";

		/// <summary>
		/// If the active player, the caller can end his turn and switch to the next player.
		/// </summary>
		public static readonly string EndTurnCommand = "EndTurn";
	}
}
