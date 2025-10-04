namespace GammonX.Server.EntityFramework
{
	/// <summary>
	/// Provides the capabilities to access entities.
	/// </summary>
	/// <typeparam name="T">Entity type to work on.</typeparam>
	public interface IRepository<T> where T : class
	{
		/// <summary>
		/// Gets the given <typeparamref name="T"/> by its <paramref name="id"/>.
		/// </summary>
		/// <param name="id">Id to filter on.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>An isntace of <typeparamref name="T"/>.</returns>
		Task<T?> GetByIdAsync(object id, CancellationToken ct = default);

		/// <summary>
		/// Gets all instances of <typeparamref name="T"/>.
		/// </summary>
		/// <param name="ct">Cancelltation token.</param>
		/// <returns>A list of instances of <typeparamref name="T"/>.</returns>
		Task<IList<T>> ListAsync(CancellationToken ct = default);

		/// <summary>
		/// Executes a query on the given table.
		/// </summary>
		/// <returns>An queryable of <typeparamref name="T"/>.</returns>
		IQueryable<T> Query();

		/// <summary>
		/// Adds the given entity <typeparamref name="T"/> to the table.
		/// </summary>
		/// <param name="entity">Entity to add.</param>
		/// <param name="ct">Cancellation token.</param>
		/// <returns>A task to be awaited.</returns>
		Task AddAsync(T entity, CancellationToken ct = default);

		/// <summary>
		/// Updates the given entity <typeparamref name="T"/> in the table.
		/// </summary>
		/// <param name="entity">Entity to update.</param>
		void Update(T entity);

		/// <summary>
		/// Removes the given entity <typeparamref name="T"/> from the table.
		/// </summary>
		/// <param name="entity">Entity to remove.</param>
		void Remove(T entity);
	}
}
