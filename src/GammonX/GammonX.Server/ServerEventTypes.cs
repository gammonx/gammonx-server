using GammonX.Server.Contracts;

namespace GammonX.Server
{
	/// <summary>
	/// Gets a list of all web socket server event types.
	/// </summary>
	internal static class ServerEventTypes
	{
		/// <summary>
		/// Invoked when a player joins a match lobby and now other player is directly found.
		/// Returns a <see cref="EventResponseContract{EventMatchLobbyPayload}"/>
		/// </summary>
		public static string MatchLobbyWaitingEvent = "match-lobby-waiting";

		/// <summary>
		/// Invoked when a match lobby is found and the player can join it.
		/// Returns a <see cref="EventResponseContract{EventMatchLobbyPayload}"/>
		/// </summary>
		public static string MatchLobbyFoundEvent = "match-lobby-found";

		/// <summary>
		/// Invoked when both players joined the match session.
		/// Returns a <see cref="EventResponseContract{EventMatchStatePayload}"/>
		/// </summary>
		public static string MatchStartedEvent = "match-started";

		/// <summary>
		/// Invoked when the active player made his last move to win the last game round.
		/// Returns a <see cref="EventResponseContract{EventMatchStatePayload}"/>
		/// </summary>
		public static string MatchEndedEvent = "match-ended";

		/// <summary>
		/// Invoked when the first player started the game round and is waiting for the second player to join.
		/// Returns a <see cref="EventResponseContract{EventMatchStatePayload}"/>
		/// </summary>
		public static string GameWaitingEvent = "game-waiting";

		/// <summary>
		/// Invoked when both players started the game.
		/// Returns a <see cref="EventResponseContract{EventGameStatePayload}"/>
		/// </summary>
		public static string GameStartedEvent = "game-started";

		/// <summary>
		/// Invoked while the game is running (e.g. Roll, Move, EndTurn commands).
		/// Returns a <see cref="EventResponseContract{EventGameStatePayload}"/>
		/// </summary>
		public static string GameStateEvent = "game-state";

		/// <summary>
		/// Invoked when the active player made his last move to win the game.
		/// Returns a <see cref="EventResponseContract{EventMatchStatePayload}"/>
		/// </summary>
		public static string GameEndedEvent = "game-ended";

		/// <summary>
		/// General error event.
		/// </summary>
		public static string ErrorEvent = "error";

		/// <summary>
		/// Event that signals the clients that they must disconnect the socket connection.
		/// </summary>
		public static string ForceDisconnect = "force-disconnect";

		/// <summary>
		/// Event that signals that the double cube owner offered a double to his opponent.
		/// </summary>
		public static string DoubleOffered = "double-offered";
	}
}
