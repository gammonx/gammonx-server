using GammonX.Models.Enums;

namespace GammonX.Server.Models
{
	/// <summary>
	/// Provides the capabilites to caputre a match session and all its actions.
	/// </summary>
	public interface IMatchHistory
	{
		/// <summary>
		/// Gets the match id.
		/// </summary>
		public Guid Id { get; }

		/// <summary>
		/// Gets the event name
		/// </summary>
		/// <remarks>
		/// e.g.
		/// "Tavli 7Point Ranked"
		/// "Backgammon Cash Normal"
		/// </remarks>
		public string Name { get; }

		/// <summary>
		/// Gets the player id of the white checker player.
		/// </summary>
		public Guid Player1 { get; }

		/// <summary>
		/// Gets the player id of the black checker player.
		/// </summary>
		public Guid Player2 { get; }

		/// <summary>
		/// Gets the start time of the match.
		/// </summary>
		public DateTime StartedAt { get; }

		/// <summary>
		/// Gets the end time of the match.
		/// </summary>
		public DateTime EndedAt { get; }

		/// <summary>
		/// Gets the match length. Each game session played adds one to the match length.
		/// </summary>
		public int Length { get; }

		/// <summary>
		/// Gets a list of game sessions played within the given match session.
		/// </summary>
		public IGameHistory[] Games { get; }

		/// <summary>
		/// Gets the type format of the string serialization.
		/// </summary>
		public HistoryFormat Format { get; }
	}
}
