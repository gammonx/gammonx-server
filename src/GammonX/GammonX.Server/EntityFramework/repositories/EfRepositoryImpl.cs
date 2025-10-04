using Microsoft.EntityFrameworkCore;

namespace GammonX.Server.EntityFramework
{
	// <inheritdoc />
	internal abstract class EfRepositoryImpl<T> : IRepository<T> where T : class
	{
		protected readonly GammonXDbContext _db;
		protected readonly DbSet<T> _set;


		public EfRepositoryImpl(GammonXDbContext db)
		{
			_db = db;
			_set = db.Set<T>();
		}

		// <inheritdoc />
		public virtual async Task<T?> GetByIdAsync(object id, CancellationToken ct = default)
		{
			return await _set.FindAsync(new[] { id }, ct);
		}

		// <inheritdoc />
		public virtual Task<IList<T>> ListAsync(CancellationToken ct = default)
		{
			return _set.ToListAsync(ct).ContinueWith(t => (IList<T>)t.Result, ct);
		}

		// <inheritdoc />
		public virtual IQueryable<T> Query() => _set.AsQueryable();

		// <inheritdoc />
		public virtual async Task AddAsync(T entity, CancellationToken ct = default)
		{
			await _set.AddAsync(entity, ct);
		}

		// <inheritdoc />
		public virtual void Update(T entity)
		{
			_set.Update(entity);
		}

		// <inheritdoc />
		public virtual void Remove(T entity)
		{
			_set.Remove(entity);
		}
	}
}
