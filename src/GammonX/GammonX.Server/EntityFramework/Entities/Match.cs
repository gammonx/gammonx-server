using GammonX.Server.Models;

namespace GammonX.Server.EntityFramework.Entities
{
	/// <summary>
	/// Represents a single match consisting of 1:n game sessions played.
	/// </summary>
	public class Match
	{
		/// <summary>
		/// Gets or sets the match id.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the match winner.
		/// </summary>
		public Player Winner { get; set; } = null!;

		/// <summary>
		/// Gets the id of the match winner. Acts as an foreign key.
		/// </summary>
		public Guid WinnerId { get; set; }

		/// <summary>
		/// Gets the points the winner earned in that match.
		/// </summary>
		public int WinnerPoints { get; set; }

		/// <summary>
		/// Gets or sets the match loser.
		/// </summary>
		public Player Loser { get; set; } = null!;

		/// <summary>
		/// Gets the id of the match loser. Acts as an foreign key.
		/// </summary>
		public Guid LoserId { get; set; }

		/// <summary>
		/// Gets the points the loser earned in that match.
		/// </summary>
		public int LoserPoints { get; set; }

		/// <summary>
		/// Gets the well known match variant (e.g. tavli)
		/// </summary>
		public WellKnownMatchVariant Variant { get; set; }

		/// <summary>
		/// Gets the well known match type (e.g. cash game or 5 point game)
		/// </summary>
		public WellKnownMatchType Type { get; set; }

		/// <summary>
		/// Gets the well known match modus (e.g. ranked or normal)
		/// </summary>
		public WellKnownMatchModus Modus { get; set; }

		/// <summary>
		/// Gets all games that the given match hosted.
		/// </summary>
		public ICollection<Game> Games { get; set; } = [];

		/// <summary>
		/// Gets the start date time of the match.
		/// </summary>
		public DateTime StartedAt { get; set; }

		/// <summary>
		/// Getst he end date time of the match.
		/// </summary>
		public DateTime EndedAt { get; set; }

		/// <summary>
		/// Gets the match length. Each played game session add to it.
		/// </summary>
		public int Length { get; set; }

		/// <summary>
		/// Gets or sets the history of the given match.
		/// </summary>
		public MatchHistory? History { get; set; }
	}
}
