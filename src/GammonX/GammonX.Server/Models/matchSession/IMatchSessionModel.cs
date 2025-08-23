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
		/// Gets a boolean indicating if the game can be started or the next round can be player.
		/// </summary>
		/// <returns>Boolean indicating if the game can be started or the next round started.</returns>
		bool CanStartNextGame();

		/// <summary>
		/// Starts a match session and the next game round.	
		/// </summary>
		/// <returns>The state of the started game round.</returns>
		IGameSessionModel StartNextGame();

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
		/// Checks if the active player can end his turn.
		/// </summary>
		/// <returns>A boolean indicating if the active player can end his turn.</returns>
		bool CanEndTurn(Guid playerId);

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
		/// Gts the current game modus of the match session.
		/// </summary>
		/// <returns>The active game modus.</returns>
		GameModus GetGameModus();

		/// <summary>
		/// Creates a game state payload which can be sent to a client.
		/// </summary>
		/// <param name="playerId">Player who receives the game state.</param>
		/// <param name="allowedCommands">A list of allowed socket commands that can follow up for the given player.</param>
		/// <returns>Returns the game state payload.</returns>
		EventGameStatePayload GetGameState(Guid playerId, params string[] allowedCommands);

		/// <summary>
		/// Constructs and returns the match state payload for both players.
		/// </summary>
		/// <param name="allowedCommands">A list of allowed socket commands that can follow up for the given player.</param>
		/// <returns>Match state payload.</returns>
		EventMatchStatePayload ToPayload(params string[] allowedCommands);
	}
}
