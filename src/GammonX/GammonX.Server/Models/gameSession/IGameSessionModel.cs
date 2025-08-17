using GammonX.Engine.Models;

using GammonX.Server.Contracts;

namespace GammonX.Server.Models
{
	/// <summary>
	/// 
	/// </summary>
	public interface IGameSessionModel
	{
		/// <summary>
		/// 
		/// </summary>
		public GameModus Modus { get; }

		/// <summary>
		/// 
		/// </summary>
		public Guid MatchId { get; }

		/// <summary>
		/// 
		/// </summary>
		public Guid Id { get; }

		/// <summary>
		/// 
		/// </summary>
		public GamePhase Phase { get; }

		/// <summary>
		/// Gets the player id of the player whos turn it is.
		/// </summary>
		public Guid ActiveTurn { get; }

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
		public DiceRollsModel DiceRollsModel { get; }

		/// <summary>
		/// Get an array of all legal moves for the active turn.
		/// </summary>
		public LegalMovesModel LegalMovesModel { get; }

		/// <summary>
		/// Gets the board model and its current state for the player1.
		/// </summary>
		/// <remarks>
		/// The board state needs to be inverted if send to the player2.
		/// </remarks>
		public IBoardModel BoardModel { get; }

		/// <summary>
		/// Game start time.
		/// </summary>
		public DateTime StartedAt { get; }

		/// <summary>
		/// Game duration in milliseconds.
		/// </summary>
		public long Duration { get; }

		/// <summary>
		/// Starts the given game session.
		/// </summary>
		/// <param name="playerId">Player id of the player which starts rolling his dices.</param>
		public void StartGame(Guid playerId);

		/// <summary>
		/// 
		/// </summary>
		public void StopGame();

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
		/// 
		/// </summary>
		/// <param name="callingPlayerId"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="isWhite"></param>
		void MoveCheckers(Guid callingPlayerId, int from, int to, bool isWhite);

		/// <summary>
		/// Creates a payload which can be sent to a client.
		/// </summary>
		/// <param name="inverted">If set to <c>true</c> the payload is created for player 2. Otherwise for player 1.</param>
		/// <returns>An instance of <see cref="EventGameStatePayload"/>.</returns>
		public EventGameStatePayload ToPayload(Guid playerId, bool inverted);
	}
}
