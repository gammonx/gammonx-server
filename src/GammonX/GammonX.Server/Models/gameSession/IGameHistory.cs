using GammonX.Engine.History;
using GammonX.Engine.Models;

namespace GammonX.Server.Models
{
	/// <summary>
	/// Provides the capabilites to caputre a game session and all its actions.
	/// </summary>
	public interface IGameHistory
	{
		/// <summary>
		/// Gets the game id.
		/// </summary>
		public Guid Id { get; }

		/// <summary>
		/// Gets the well known modus of the board.
		/// </summary>
		public GameModus Modus { get; }

		/// <summary>
		/// Gets the earned point of the game winner.
		/// </summary>
		public int Points { get; }

		/// <summary>
		/// Gets the player id of the winner.
		/// </summary>
		public Guid WinnerPlayerId { get; }

		/// <summary>
		/// Gets the start time of the game.
		/// </summary>
		public DateTime StartedAt { get; }

		/// <summary>
		/// Gets the end time of the game.
		/// </summary>
		public DateTime EndedAt { get; }

		/// <summary>
		/// Getst the board history.
		/// </summary>
		public IBoardHistory BoardHistory { get; }
	}
}
