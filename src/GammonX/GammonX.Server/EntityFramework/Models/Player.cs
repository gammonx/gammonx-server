namespace GammonX.Server.EntityFramework.Models
{
	/// <summary>
	/// Represents a player authenticated by an external service.
	/// </summary>
	public class Player
	{
		/// <summary>
		/// Gets or sets the id of the player.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Gets all matches that the given player won.
		/// </summary>
		public ICollection<Match> WonMatches { get; set; } = [];

		/// <summary>
		/// Gets all matches that the given player lost.
		/// </summary>
		public ICollection<Match> LostMatches { get; set; } = [];

		/// <summary>
		/// Gets all games that the given player won.
		/// </summary>
		public ICollection<Game> GamesWon { get; set; } = new List<Game>();

		/// <summary>
		/// Gets all games that the given player lost.
		/// </summary>
		public ICollection<Game> GamesLost { get; set; } = new List<Game>();

		/// <summary>
		/// Gets or sets the ratings the given player has in the different match variants and types.
		/// </summary>
		public ICollection<PlayerRating> Ratings { get; set; } = new List<PlayerRating>();

		/// <summary>
		/// Gets or sets the stats the given player has in the different match variants, types and modus.
		/// </summary>
		public ICollection<PlayerStats> Stats { get; set; } = new List<PlayerStats>();
	}
}
