using GammonX.Server.EntityFramework.Entities;

using Microsoft.EntityFrameworkCore;

namespace GammonX.Server.EntityFramework
{
	/// <summary>
	/// Provides specific access capabilities for the <see cref="Match"/> entity.
	/// </summary>
	public interface IMatchRepository : IRepository<Match>
	{
		/// <summary>
		/// Gets the given match with <paramref name="id"/> and includes its <see cref="Game"/>.
		/// </summary>
		/// <param name="id">Id of the match to fetch.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>If a match with id is found. Otherwise <c>null</c>.</returns>
		Task<Match?> GetWithGamesAsync(Guid id, CancellationToken ct = default);

		/// <summary>
		/// Gets the given match with <paramref name="id"/> and includes its <see cref="MatchHistory"/>.
		/// </summary>
		/// <param name="id">Id of the match to fetch.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>If a match with id is found. Otherwise <c>null</c>.</returns>
		Task<Match?> GetWithHistoryAsync(Guid id, CancellationToken ct = default);

		/// <summary>
		/// Gets the given match with <paramref name="id"/> and includes its games and history.
		/// </summary>
		/// <param name="id">Id of the match to fetch.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>If a match with id is found. Otherwise <c>null</c>.</returns>
		Task<Match?> GetFullMatchAsync(Guid id, CancellationToken ct = default);
	}

	// <inheritdoc />
	internal sealed class MatchRepositoryImpl : EfRepositoryImpl<Match>, IMatchRepository
	{
		public MatchRepositoryImpl(GammonXDbContext db) : base(db) { }

		// <inheritdoc />
		public async Task<Match?> GetFullMatchAsync(Guid id, CancellationToken ct = default)
		{
			return await _db.Matches
				.Include(m => m.History)
				.Include(m => m.Winner)
				.Include(m => m.Loser)
				.Include(m => m.Games)
				.ThenInclude(m => m.History)
				.Include(m => m.Games)
				.ThenInclude(m => m.Winner)
				.FirstOrDefaultAsync(m => m.Id == id, ct);
		}

		// <inheritdoc />
		public async Task<Match?> GetWithGamesAsync(Guid id, CancellationToken ct = default)
		{
			return await _db.Matches
				.Include(m => m.Winner)
				.Include(m => m.Loser)
				.Include(m => m.Games)
				.ThenInclude(g => g.Winner)
				.Include(m => m.Games)
				.ThenInclude(m => m.History)
				.FirstOrDefaultAsync(m => m.Id == id, ct);
		}

		// <inheritdoc />
		public async Task<Match?> GetWithHistoryAsync(Guid id, CancellationToken ct = default)
		{
			return await _db.Matches
				.Include(m => m.History)
				.Include(m => m.Winner)
				.Include(m => m.Loser)
				.FirstOrDefaultAsync(m => m.Id == id, ct);
		}
	}
}
