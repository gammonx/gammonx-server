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
        /// Invoked when both players joined the match session and have not yet started the match (rolled their opening dice).
        /// Returns a <see cref="EventResponseContract{EventMatchStatePayload}"/>
        /// </summary>
        public static string MatchWaitingForStartEvent = "match-waiting-for-start";

        /// <summary>
        /// Invoked when the first player started the match and rolled their opening dice.
        /// Returns a <see cref="EventResponseContract{EventMatchStatePayload}"/>
        /// </summary>
        public static string MatchWaitingEvent = "match-waiting";

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
        /// Invoked when the active player requests an up to date match state.
        /// Returns a <see cref="EventResponseContract{EventMatchStatePayload}"/>
        /// </summary>
        public static string MatchStateEvent = "match-state";

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
        /// Event that signals the clients that their opponent has disconnected from the match.
        /// </summary>
        public static string PlayerDisconnectedEvent = "player-disconnected";

        /// <summary>
        /// Event that signals the clients that their opponent has reconnected to the match.
        /// </summary>
        public static string PlayerConnectedEvent = "player-connected";

        /// <summary>
        /// Event that signals the clients that they must disconnect the socket connection.
        /// </summary>
        public static string ForceDisconnectEvent = "force-disconnect";

		/// <summary>
		/// Event that signals that the double cube owner offered a double to his opponent.
		/// </summary>
		public static string DoubleOfferedEvent = "double-offered";

        /// <summary>
        /// Event that signals that the offered player accepted the double offer.
        /// </summary>
        public static string DoubleAcceptedEvent = "double-accepted";

        /// <summary>
        /// Event that signals that the active player has a turn timer running and the remaining time is updated.
        /// </summary>
        public static string TurnTimerEvent = "turn-timer";
    }
}
