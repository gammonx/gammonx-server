using GammonX.Engine.Models;

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
		WellKnownMatchVariant Variant { get; }

		/// <summary>
		/// Gets the player 1.
		/// </summary>
		PlayerModel Player1 { get; }
		
		/// <summary>
		/// Gets the player 2.
		/// </summary>
		PlayerModel Player2 { get; }

		/// <summary>
		/// Gets the match start time.
		/// </summary>
		DateTime StartedAt { get; }

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
		/// Starts a match session for the current game round.
		/// </summary>
		/// <returns>The state of the started game round.</returns>
		IGameSessionModel StartMatch();

		/// <summary>
		/// Starts the next game round in the match session and returns the state of the game session for the next round.
		/// </summary>
		/// <returns>The state of the next game round.</returns>
		IGameSessionModel NextGameRound();

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
		void MoveCheckers(Guid callingPlayerId, int from, int to);

		/// <summary>
		/// The active player has finished his turn and the next player is now active.
		/// </summary>
		/// <param name="callingPlayerId">Player to finish his turn.</param>
		void EndTurn(Guid callingPlayerId);

		/// <summary>
		/// Creates a game state payload which can be sent to a client.
		/// </summary>
		/// <param name="playerId">Player who receives the game state.</param>
		/// <param name="allowedCommands">A list of allowed socket commands that can follow up for the given player.</param>
		/// <returns>Returns the game state payload.</returns>
		EventGameStatePayload GetGameState(Guid playerId, params string[] allowedCommands);

		/// <summary>
		/// Gts the current game modus of the match session.
		/// </summary>
		/// <returns>The active game modus.</returns>
		GameModus GetGameModus();

		/// <summary>
		/// Gets a boolean indicating if the game can be started.
		/// </summary>
		/// <returns>Boolean indicating if the game can be started.</returns>
		bool CanStartGame();

		/// <summary>
		/// Constructs and returns the match state payload for both players.
		/// </summary>
		/// <param name="allowedCommands">A list of allowed socket commands that can follow up for the given player.</param>
		/// <returns>Match state payload.</returns>
		EventMatchStatePayload ToPayload(params string[] allowedCommands);

		/// <summary>
		/// Checks if the active player can end his turn.
		/// </summary>
		/// <returns>A boolean indicating if the active player can end his turn.</returns>
		bool CanEndTurn(Guid playerId);
	}
}
