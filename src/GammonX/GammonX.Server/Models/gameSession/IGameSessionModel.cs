using GammonX.Engine.Models;

using GammonX.Models.Enums;

using GammonX.Server.Contracts;

namespace GammonX.Server.Models
{
	/// <summary>
	/// Represents a game session model that contains all information about a single game session.
	/// </summary>
	public interface IGameSessionModel
	{
		/// <summary>
		/// Gets the modus of this game round.
		/// </summary>
		public GameModus Modus { get; }

		/// <summary>
		/// Gets the match id of the match this game session belongs to.
		/// </summary>
		public Guid MatchId { get; }

		/// <summary>
		/// Gets the game id of this game session.
		/// </summary>
		public Guid Id { get; }

		/// <summary>
		/// Gets the current game phase.
		/// </summary>
		/// <remarks>
		/// The game phase is always different for each player and indicates which player is currently active and what he is allowed to do.
		/// </remarks>
		public GamePhase Phase { get; }

		/// <summary>
		/// Gets the player id of the player whos turn it is.
		/// </summary>
		public Guid ActivePlayer { get; }

		/// <summary>
		/// Gets the player id of the player who is not the active player.
		/// </summary>
		public Guid OtherPlayer { get; }

		/// <summary>
		/// Gets the amount of turns already played.
		/// </summary>
		/// <remarks>
		/// Each time a player rolls his dices, the turn number increases by 1.
		/// </remarks>
		public int TurnNumber { get; }

		/// <summary>
		/// Get an array of all dice rolls for the active turn.
		/// </summary>
		public DiceRollsModel DiceRolls { get; }

		/// <summary>
		/// Get an array of all legal moves for the active turn.
		/// </summary>
		public MoveSequences  MoveSequences { get; }

		/// <summary>
		/// Gets the board model and its current state for the player1.
		/// </summary>
		/// <remarks>
		/// The board state needs to be inverted if send to the player2.
		/// </remarks>
		public IBoardModel BoardModel { get; }

		/// <summary>
		/// Gets game utc start time.
		/// </summary>
		public DateTime StartedAt { get; }

		/// <summary>
		/// Gets game utc end/stop time.
		/// </summary>
		public DateTime EndedAt { get; }

		/// <summary>
		/// Game duration in milliseconds.
		/// </summary>
		public long Duration { get; }

		/// <summary>
		/// Starts the given game session.
		/// </summary>
		/// <param name="activePlayer">Player id of the player which starts rolling his dices.</param>
		/// <param name="otherPLayer">Other player id which is not actively playing.</param>
		public void StartGame(Guid activePlayer, Guid otherPLayer);

		/// <summary>
		/// Stops the current game session.
		/// </summary>
		/// <param name="winner">Player id who won the game.</param>
		/// <param name="score">Score achieved by the winner.</param>
		public void StopGame(Guid winner, int score);

		/// <summary>
		/// The player1 rolled his dices and made his moves. Now the active player is switched to player2.
		/// </summary>
		/// <param name="playerId">Player id of the player which is the next active player.</param>
		public void NextTurn(Guid playerId);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="playerId"></param>
		/// <param name="isWhite"></param>
		void RollDices(Guid playerId, bool isWhite);

		/// <summary>
		/// Moves the checkers of the active player <paramref name="callingPlayerId"/> from one position to another.
		/// The given <paramref name="from"/> <paramref name="to"/> move can be a single move sequence or a combined move sequence.
		/// </summary>
		/// <remarks>
		/// Validates if the given <paramref name="callingPlayerId"/> is actually the active player and if the move is valid.
		/// </remarks>
		/// <param name="callingPlayerId">Active player.</param>
		/// <param name="from">Board array from index.</param>
		/// <param name="to">Board array to index.</param>
		/// <param name="isWhite">Indicates if the <paramref name="callingPlayerId"/> playing the white checkers. False if black checkers.</param>
		void MoveCheckers(Guid callingPlayerId, int from, int to, bool isWhite);

		/// <summary>
		/// Undoes the last move made by the given player with <paramref name="callingPlayerId"/>.
		/// </summary>
		/// <param name="callingPlayerId">Player to undo the last move.</param>
		/// <param name="isWhite">Indicates if the <paramref name="callingPlayerId"/> playing the white checkers. False if black checkers.</param>
		void UndoLastMove(Guid callingPlayerId, bool isWhite);

		/// <summary>
		/// Checks if the given <paramref name="callingPlayerId"/> can undo hist last move.
		/// </summary>
		/// <param name="callingPlayerId">Player to undo his last move.</param>
		/// <returns>Returns <c>true</c> if player can undo hist last move. Otherwise, <c>false</c>.</returns>
		bool CanUndoLastMove(Guid callingPlayerId);

		/// <summary>
		/// Checks if the given game session is over.
		/// </summary>
		/// <param name="isWhite">True for white checkers. False for black checkers</param>
		/// <returns>A boolean indicating if the active game session is over.</returns>
		bool GameOver(bool isWhite);

		/// <summary>
		/// Creates a payload which can be sent to a client.
		/// </summary>
		/// <param name="playerId">Player for which the payload is constructed.</param>
		/// <param name="allowedCommands">A list of allowed commands for the client.</param>
		/// <param name="inverted">If set to <c>true</c> the payload is created for player 2. Otherwise for player 1.</param>
		/// <returns>An instance of <see cref="EventGameStatePayload"/>.</returns>
		public EventGameStatePayload ToPayload(Guid playerId, string[] allowedCommands, bool inverted);

		/// <summary>
		/// Creates a game round contract which concludes the game session.
		/// </summary>
		/// <param name="gameRoundIndex">Index of the game session played in the match session.</param>
		/// <returns>An instance of <see cref="GameRoundContract"/>.</returns>
		public GameRoundContract ToContract(int gameRoundIndex);

		/// <summary>
		/// Gets the game history including all move and roll events.
		/// </summary>
		/// <param name="player1">Gets the id of the player with the white checkers.</param>
		/// <param name="player2">Gets the id of the player with the black checkers.</param>
		/// <returns>Returns an instance of <see cref="IGameHistory"/>.</returns>
		IGameHistory GetHistory(Guid player1, Guid player2);
	}
}
