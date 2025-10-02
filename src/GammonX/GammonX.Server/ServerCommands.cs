namespace GammonX.Server
{
	/// <summary>
	/// Gets a list of all commands the web socket hub can receive.
	/// </summary>
	public static class ServerCommands
	{
		/// <summary>
		/// The caller can join a match session with an existing match id created by the matchmaking service.
		/// </summary>
		public const string JoinMatchCommand = "JoinMatch";

		/// <summary>
		/// The caller can start a game round if successfuly joined a match session.
		/// </summary>
		public const string StartGameCommand = "StartGame";

		/// <summary>
		/// If the active player, the caller can roll the dices and start his turn.
		/// </summary>
		public const string RollCommand = "Roll";

		/// <summary>
		/// If the active player, the caller can move his checkers from one position to another.
		/// </summary>
		public const string MoveCommand = "Move";

		/// <summary>
		/// If the active player, the caller can undo hist last move.
		/// </summary>
		public const string UndoMoveCommand = "UndoMove";

		/// <summary>
		/// If the active player, the caller can end his turn and switch to the next player.
		/// </summary>
		public const string EndTurnCommand = "EndTurn";

		/// <summary>
		/// The calling player resigns the match and loses automatically.
		/// </summary>
		public const string ResignMatchCommand = "ResignMatch";

		/// <summary>
		/// The calling player resigns the game and loses as a back-/gammon.
		/// </summary>
		public const string ResignGameCommand = "ResignGame";

		/// <summary>
		/// The doubling cube owner offers a double.
		/// </summary>
		public const string OfferDoubleCommands = "OfferDouble";

		/// <summary>
		/// The non doubling cube owner accepts the double offering.
		/// </summary>
		public const string AcceptDoubleCommands = "AcceptDouble";

		/// <summary>
		/// The non doubling cube owner declines the double offering and automatically loses as a backgammon.
		/// </summary>
		public const string DeclineDoubleCommands = "DeclineDouble";
	}
}
