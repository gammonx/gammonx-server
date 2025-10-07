using GammonX.Server.EntityFramework.Entities;

namespace GammonX.Server.EntityFramework.Services
{
	/// <summary>
	/// Provides the CRUD capabilties for the <see cref="Player"/> entity.
	/// </summary>
	public interface IPlayerService
	{
		/// <summary>
		/// Gets a player by the given <paramref name="id"/>.
		/// </summary>
		/// <param name="id">If of the player to fetch.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>An instance of <see cref="Player"/> or <c>null</c> if not found.</returns>
		Task<Player?> GetAsync(Guid id, CancellationToken ct = default);

		/// <summary>
		/// Gets a player by the given <paramref name="id"/> including its <see cref="PlayerRating"/>.
		/// </summary>
		/// <param name="id">If of the player to fetch.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>An instance of <see cref="Player"/> or <c>null</c> if not found.</returns>
		Task<Player?> GetWithRatingAsync(Guid id, CancellationToken ct = default);

		Task<Guid> CreateAsync(Guid id, string userName, CancellationToken ct = default);

		Task<bool> RemovePlayerAsync(Guid id, CancellationToken ct = default);
	}
}
