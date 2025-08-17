using GammonX.Engine.Models;

using GammonX.Server.Contracts;

namespace GammonX.Server.Models
{
	/// <summary>
	/// 
	/// </summary>
	public interface IMatchSessionModel
	{
		/// <summary>
		/// 
		/// </summary>
		public Guid Id { get; }

		/// <summary>
		/// 
		/// </summary>
		public int GameRound { get; }

		/// <summary>
		/// 
		/// </summary>
		public WellKnownMatchVariant Variant { get; }

		/// <summary>
		/// 
		/// </summary>
		public PlayerModel Player1 { get; }
		
		/// <summary>
		/// 
		/// </summary>
		public PlayerModel Player2 { get; }

		/// <summary>
		/// Match start time.
		/// </summary>
		public DateTime StartedAt { get; }

		/// <summary>
		/// Match duration in milliseconds.
		/// </summary>
		public long Duration { get; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="player"></param>
		public void JoinSession(LobbyEntry player);

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public IGameSessionModel StartMatch();

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public IGameSessionModel NextGameRound();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="callingPlayerId"></param>
		void RollDices(Guid callingPlayerId);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="callingPlayerId"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		void MoveCheckers(Guid callingPlayerId, int from, int to);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="playerId"></param>
		/// <returns></returns>
		EventGameStatePayload GetGameState(Guid playerId);

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public GameModus GetGameModus();

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public bool CanStartGame();

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public EventMatchStatePayload ToPayload();
	}
}
