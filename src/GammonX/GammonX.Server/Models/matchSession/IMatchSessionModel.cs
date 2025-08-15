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
		/// <returns></returns>
		IGameSessionModel GetGameSession();

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public GameModus GetGameModus();

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public MatchStatePayload ToPayload();
	}
}
