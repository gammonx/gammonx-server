using GammonX.Server.EntityFramework.Entities;

using Microsoft.EntityFrameworkCore;

namespace GammonX.Server.EntityFramework
{
	/// <summary>
	/// Provides specific access capabilities for the <see cref="Player"/> entity.
	/// </summary>
	public interface IPlayerRepository : IRepository<Player>
	{
		/// <summary>
		/// Gets the given player with <paramref name="id"/> and includes its <see cref="PlayerStats"/>.
		/// </summary>
		/// <param name="id">Id of the player to fetch.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>If a player with id is found. Otherwise <c>null</c>.</returns>
		Task<Player?> GetWithStatsAsync(Guid id, CancellationToken ct = default);

		/// <summary>
		/// Gets the given player with <paramref name="id"/> and includes its <see cref="PlayerRating"/>.
		/// </summary>
		/// <param name="id">Id of the player to fetch.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>If a player with id is found. Otherwise <c>null</c>.</returns>
		Task<Player?> GetWithRatingAsync(Guid id, CancellationToken ct = default);

		/// <summary>
		/// Gets the given player with <paramref name="id"/> and includes its <see cref="Match"/>.
		/// </summary>
		/// <param name="id">Id of the player to fetch.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>If a player with id is found. Otherwise <c>null</c>.</returns>
		Task<Player?> GetWithMatchesAsync(Guid id, CancellationToken ct = default);

		/// <summary>
		/// Gets the given player with <paramref name="id"/> and includes its <see cref="Game"/>.
		/// </summary>
		/// <param name="id">Id of the player to fetch.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>If a player with id is found. Otherwise <c>null</c>.</returns>
		Task<Player?> GetWithGamesAsync(Guid id, CancellationToken ct = default);

		/// <summary>
		/// Gets the given player with <paramref name="id"/> and includes its stats, ratings, matches and games.
		/// </summary>
		/// <param name="id">Id of the player to fetch.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>If a player with id is found. Otherwise <c>null</c>.</returns>
		Task<Player?> GetFullPlayerAsync(Guid id, CancellationToken ct = default);
	}


	// <inheritdoc />
	internal sealed class PlayerRepositoryImpl : EfRepositoryImpl<Player>, IPlayerRepository
	{
		public PlayerRepositoryImpl(GammonXDbContext db) : base(db) { }

		// <inheritdoc />
		public async Task<Player?> GetWithStatsAsync(Guid id, CancellationToken ct = default)
		{
			return await _db.Players
				.Include(p => p.Stats)
				.FirstOrDefaultAsync(p => p.Id == id, ct);
		}

		// <inheritdoc />
		public async Task<Player?> GetWithRatingAsync(Guid id, CancellationToken ct = default)
		{
			return await _db.Players
				.Include(p => p.Ratings)
				.FirstOrDefaultAsync(p => p.Id == id, ct);
		}

		// <inheritdoc />
		public async Task<Player?> GetWithMatchesAsync(Guid id, CancellationToken ct = default)
		{
			return await _db.Players
				.Include(p => p.WonMatches)
				.ThenInclude(m => m.History)
				.Include(p => p.LostMatches)
				.ThenInclude(m => m.History)
				.FirstOrDefaultAsync(p => p.Id == id, ct);
		}

		// <inheritdoc />
		public async Task<Player?> GetWithGamesAsync(Guid id, CancellationToken ct = default)
		{
			return await _db.Players
				.Include(p => p.GamesWon)
				.ThenInclude(g => g.History)
				.Include(p => p.GamesLost)
				.ThenInclude(g => g.History)
				.FirstOrDefaultAsync(p => p.Id == id, ct);
		}

		// <inheritdoc />
		public async Task<Player?> GetFullPlayerAsync(Guid id, CancellationToken ct = default)
		{
			return await _db.Players
				.Include(p => p.Stats)
				.Include(p => p.Ratings)
				.Include(p => p.WonMatches)
				.ThenInclude(m => m.History)
				.Include(p => p.LostMatches)
				.ThenInclude(m => m.History)
				.Include(p => p.GamesWon)
				.ThenInclude(g => g.History)
				.Include(p => p.GamesLost)
				.ThenInclude(g => g.History)
				.FirstOrDefaultAsync(p => p.Id == id, ct);
		}
	}
}
