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
		Task<Player?> GetPlayerAsync(Guid id, CancellationToken ct = default);

		Task<Guid> CreatePlayerAsync(Guid id, CancellationToken ct = default);
	}
}
