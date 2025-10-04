namespace GammonX.Server.EntityFramework
{
	/// <summary>
	/// Provides capabilities to execute some work against the database.
	/// </summary>
	public interface IUnitOfWork : IDisposable
	{
		/// <summary>
		/// Gets the repository for <see cref="Entities.Player"/> entity.
		/// </summary>
		IPlayerRepository Players { get; }

		/// <summary>
		/// Gets the repository for the <see cref="Entities.Match"/>
		/// </summary>
		IMatchRepository Matches { get; }

		/// <summary>
		/// Saves the changes to the db context.
		/// </summary>
		/// <param name="ct">Cancellation token.</param>
		/// <returns></returns>
		Task<int> SaveChangesAsync(CancellationToken ct = default);

		/// <summary>
		/// Begins a transaction.
		/// </summary>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A task to be awaited.</returns>
		Task BeginTransactionAsync(CancellationToken ct = default);

		/// <summary>
		/// Commits changes to the db context.
		/// </summary>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A task to be awaited.</returns>
		Task CommitAsync(CancellationToken ct = default);

		/// <summary>
		/// Roll back the most recent changes.
		/// </summary>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A task to be awaited.</returns>
		Task RollbackAsync(CancellationToken ct = default);
	}
}
