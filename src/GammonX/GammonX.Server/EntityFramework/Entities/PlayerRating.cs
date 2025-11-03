using GammonX.Server.Models;

namespace GammonX.Server.EntityFramework.Entities
{
	/// <summary>
	/// Provides all information for a player rating for the given match variant and type.
	/// The player rating only applies to matches of type <see cref="WellKnownMatchModus.Ranked"/>
	/// </summary>
	public class PlayerRating
	{
		/// <summary>
		/// Gets or sets the id of the given player rating.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the id of the player this rating belongs to. Act as a foreign key.
		/// </summary>
		public Guid PlayerId { get; set; }

		/// <summary>
		/// Gets or sets the player this rating belongs to.
		/// </summary>
		public Player Player { get; set; } = null!;

		/// <summary>
		/// Gets or sets the match variant this rating belongs to.
		/// </summary>
		public WellKnownMatchVariant Variant { get; set; }

		/// <summary>
		/// Gets or sets the match type this rating belongs to.
		/// </summary>
		public WellKnownMatchType Type { get; set; }

		/// <summary>
		/// Gets or sets the rating for the given playe in the given variant.
		/// </summary>
		public double Rating { get; set; }

		/// <summary>
		/// Gets or sets the lowest rating for the given player in variant/type.
		/// </summary>
		public double LowestRating { get; set; }

		/// <summary>
		/// Gets or sets the highest rating for the given player in variant/type
		/// </summary>
		public double HighestRating { get; set; }

		/// <summary>
		/// Gets or sets the amount of matches played by the given player for this variant.
		/// </summary>
		public int MatchesPlayed { get; set; }
	}
}
