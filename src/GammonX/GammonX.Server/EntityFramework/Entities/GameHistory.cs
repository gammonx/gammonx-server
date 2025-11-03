using System.ComponentModel.DataAnnotations;

namespace GammonX.Server.EntityFramework.Entities
{
	/// <summary>
	/// Provides the information to replay or analyze a game session.
	/// </summary>
	public class GameHistory
	{

		/// <summary>
		/// Gets or sets the id of the related game. Acts as a foreign key.
		/// </summary>
		[Key]
		public Guid GameId { get; set; }

		/// <summary>
		/// Gets or sets the related game session.
		/// </summary>
		public Game Game { get; set; } = null!;

		/// <summary>
		/// Gets or sets the history of the played board in the given <see cref="Format"/>.
		/// </summary>
		public string Data { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the format of the given <see cref="Data"/>.
		/// </summary>
		public string Format { get; set; } = "MAT";
	}
}
