using System.ComponentModel.DataAnnotations;

namespace GammonX.Server.EntityFramework.Models
{
	/// <summary>
	/// Provides the information to replay or analyze a match session.
	/// </summary>
	public class MatchHistory
	{
		/// <summary>
		/// Gets or sets the id of the related match. Acts as a foreign key.
		/// </summary>
		[Key]
		public Guid MatchId { get; set; }

		/// <summary>
		/// Gets or sets the related match session.
		/// </summary>
		public Match Match { get; set; } = null!;

		/// <summary>
		/// Gets or sets the history of the played match in the given <see cref="Format"/>.
		/// </summary>
		public string Data { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the format of the given <see cref="Data"/>.
		/// </summary>
		public string Format { get; set; } = "MAT";
	}
}
