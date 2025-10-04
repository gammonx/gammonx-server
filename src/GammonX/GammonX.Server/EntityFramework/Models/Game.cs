using GammonX.Engine.Models;

namespace GammonX.Server.EntityFramework.Models
{
	/// <summary>
	/// Represents a single game consisting played within a match.
	/// </summary>
	public class Game
	{
		/// <summary>
		/// Gets or sets the game id.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the related match id. Acts as a foreign key.
		/// </summary>
		public Guid MatchId { get; set; }

		/// <summary>
		/// Gets or sets the related match instance.
		/// </summary>
		public Match Match { get; set; } = null!;

		/// <summary>
		/// Gets the well known modus of the board.
		/// </summary>
		public GameModus Modus { get; set; }

		/// <summary>
		/// Gets the earned point of the game winner.
		/// </summary>
		public int Points { get; set; }

		/// <summary>
		/// Gets the player who won the game.
		/// </summary>
		public Player Winner { get; set; } = null!;

		/// <summary>
		/// Gets the id of the match winner. Acts as an foreign key.
		/// </summary>
		public Guid WinnerId { get; set; }

		/// <summary>
		/// Gets the player wo lost the game.
		/// </summary>
		public Player Loser { get; set; } = null!;

		/// <summary>
		/// Gets the id of the match loser. Acts as an foreign key.
		/// </summary>
		public Guid LoserId { get; set; }

		/// <summary>
		/// Gets the start time of the game.
		/// </summary>
		public DateTime StartedAt { get; set; }

		/// <summary>
		/// Gets the end time of the game.
		/// </summary>
		public DateTime EndedAt { get; set; }

		/// <summary>
		/// Gets or sets the history of the given game.
		/// </summary>
		public GameHistory? History { get; set; }
	}
}
