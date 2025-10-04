using GammonX.Server.EntityFramework.Entities;

namespace GammonX.Server.EntityFramework.Services
{
	/// <summary>
	/// Provides the CRUD capabilties for the <see cref="Match"/> entity.
	/// </summary>
	public interface IMatchService
	{
		/// <summary>
		/// Gets a match by the given <paramref name="id"/>.
		/// </summary>
		/// <param name="id">If of the match to fetch.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>An instance of <see cref="Match"/> or <c>null</c> if not found.</returns>
		Task<Match?> GetMatchAsync(Guid id, CancellationToken ct = default);

		Task<Guid> CreateMatchAsync(Guid id, CancellationToken ct = default);
	}
}
