using GammonX.Models.Enums;

using GammonX.Server.Contracts;

namespace GammonX.Server.Models
{
	/// <summary>
	/// Represents a match session model that contains all information about a match session.
	/// </summary>
	public interface IMatchSessionModel
	{
		/// <summary>
		/// Gets the match id of this match session.
		/// </summary>
		Guid Id { get; }

		/// <summary>
		/// Gets the game round.
		/// </summary>
		int GameRound { get; }

		/// <summary>
		/// Gets the match variant determining the type and amount of games played in this match session.
		/// </summary>
		MatchVariant Variant { get; }

		/// <summary>
		/// Gets the modus of the match.
		/// </summary>
		MatchModus Modus { get; }

		/// <summary>
		/// Gets the type of the match determining the winning condition.
		/// </summary>
		GammonX.Models.Enums.MatchType Type { get; }

		/// <summary>
		/// Gets the player 1.
		/// </summary>
		MatchPlayerModel Player1 { get; }
		
		/// <summary>
		/// Gets the player 2.
		/// </summary>
		MatchPlayerModel Player2 { get; }

		/// <summary>
		/// Gets the match start time.
		/// </summary>
		DateTime StartedAt { get; }

		/// <summary>
		/// Gets game utc end/stop time.
		/// </summary>
		public DateTime? EndedAt { get; }

		/// <summary>
		/// Gets the match duration in milliseconds.
		/// </summary>
		long Duration { get; }

		/// <summary>
		/// Joins a player to the match session.
		/// </summary>
		/// <param name="player">Play to join the match session.</param>
		void JoinSession(LobbyEntry player);

        /// <summary>
        /// Gets a boolean indicating if the game can be started or the next round can be player.
        /// </summary>
        /// <returns>Boolean indicating if the game can be started or the next round started.</returns>
        bool CanStartNextGame();

        /// <summary>
        /// Starts the match session by implicitly starting the first game round.
        /// </summary>
        /// <param name="callingPlayerId">Id of the player who is starting to roll the dices.</param>
        /// <returns>The state of the started game round.</returns>
        IGameSessionModel StartMatch(Guid callingPlayerId);

        /// <summary>
        /// Starts the next game round in the match session.
        /// </summary>
		/// <remarks>
		/// Shall be used for all subsequent game rounds after the first game round has been started with <see cref="StartMatch(Guid)"/>.
		/// </remarks>
        /// <param name="callingPlayerId">Id of the player who is starting to roll the dices.</param>
        /// <returns>The state of the started game round.</returns>
        IGameSessionModel StartNextGame(Guid callingPlayerId);

		/// <summary>
		/// Rolls the dices for the active player in the current game session and updates the game state accordingly.
		/// </summary>
		/// <param name="callingPlayerId">Player to roll his dices.</param>
		void RollDices(Guid callingPlayerId);

		/// <summary>
		/// Moves the checkers of the active player <paramref name="callingPlayerId"/> from one position to another.
		/// </summary>
		/// <param name="callingPlayerId">Player to move his checkers.</param>
		/// <param name="from">From board array index.</param>
		/// <param name="to">To board array index.</param>
		/// <returns>Boolean indicating if the made move wins the game for the active player.</returns>
		bool MoveCheckers(Guid callingPlayerId, int from, int to);

		/// <summary>
		/// Undoes the last move made by the given player with <paramref name="callingPlayerId"/>.
		/// </summary>
		/// <param name="callingPlayerId">Player to undo the last move.</param>
		void UndoLastMove(Guid callingPlayerId);

		/// <summary>
		/// Checks if the given <paramref name="callingPlayerId"/> can undo hist last move.
		/// </summary>
		/// <param name="callingPlayerId">Player to undo his last move.</param>
		/// <returns>Returns <c>true</c> if player can undo hist last move. Otherwise, <c>false</c>.</returns>
		bool CanUndoLastMove(Guid callingPlayerId);

		/// <summary>
		/// Checks if the active player can end his turn.
		/// </summary>
		/// <returns>A boolean indicating if the active player can end his turn.</returns>
		bool CanEndTurn(Guid callingPlayerId);

		/// <summary>
		/// The active player has finished his turn and the next player is now active.
		/// </summary>
		/// <param name="callingPlayerId">Player to finish his turn.</param>
		void EndTurn(Guid callingPlayerId);

		/// <summary>
		/// The calling player resigns the game resulting in a variant specific amount of points for the other player.
		/// </summary>
		/// <remarks>
		/// The player who resigns the game must not be the active player. Resigning is possible at any time.
		/// </remarks>
		/// <param name="callingPlayerId">Player who resigns the game.</param>
		void ResignGame(Guid callingPlayerId);

		/// <summary>
		/// The calling player resigns the match resulting in a variant specific amount of points per game round unplayed for the other player.
		/// </summary>
		/// <remarks>
		/// The player who resigns the game must not be the active player. Resigning is possible at any time.
		/// </remarks>
		/// <param name="callingPlayerId">Player who resigns the match.</param>
		void ResignMatch(Guid callingPlayerId);

		/// <summary>
		/// Checks if all games are player and over
		/// </summary>
		/// <returns>Boolean indicating if the match is over.</returns>
		bool IsMatchOver();

		/// <summary>
		/// Calculates how many points the calling player needs to win the match.
		/// </summary>
		/// <param name="callingPlayerId">Calling player id.</param>
		/// <returns>Amount of points needed to win the match.</returns>
		int PointsAway(Guid callingPlayerId);

		/// <summary>
		/// Gts the current game modus of the match session.
		/// </summary>
		/// <returns>The active game modus.</returns>
		GameModus GetGameModus();

		/// <summary>
		/// Returns the game session for the given <paramref name="gameRound"/>.
		/// </summary>
		/// <param name="gameRound">Game round.</param>
		/// <returns>An instance of <see cref="IGameSessionModel"/> or null if game session not yet started.</returns>
		IGameSessionModel? GetGameSession(int gameRound);

		/// <summary>
		/// Returns all game sessions of this match session.
		/// </summary>
		/// <returns>An array of <see cref="IGameSessionModel"/> instances.</returns>
		IGameSessionModel[] GetGameSessions();

		/// <summary>
		/// Creates a game state payload which can be sent to a client.
		/// </summary>
		/// <param name="callingPlayerId">Player who receives the game state.</param>
		/// <returns>Returns the game state payload.</returns>
		EventGameStatePayload GetGameState(Guid callingPlayerId);

		/// <summary>
		/// Constructs and returns the match state payload for both players.
		/// </summary>
		/// <param name="callingPlayerId">Player who receives the game state.</param>
		/// <returns>Match state payload.</returns>
		EventMatchStatePayload ToPayload(Guid callingPlayerId);

		/// <summary>
		/// Gets the history of all contained gamesession, board and the match itself.
		/// </summary>
		/// <returns>Returns an instance of <see cref="IMatchHistory"/>.</returns>
		IMatchHistory GetHistory();
	}
}
