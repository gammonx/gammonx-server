using Microsoft.EntityFrameworkCore.Storage;

namespace GammonX.Server.EntityFramework
{
	// <inheritdoc />
	public sealed class UnitOfWork : IUnitOfWork
	{
		private readonly GammonXDbContext _db;
		private IDbContextTransaction? _transaction;


		public UnitOfWork(GammonXDbContext db)
		{
			_db = db;
			Players = new PlayerRepositoryImpl(db);
			Matches = new MatchRepositoryImpl(db);
		}

		// <inheritdoc />
		public IPlayerRepository Players { get; }

		// <inheritdoc />
		public IMatchRepository Matches { get; }

		// <inheritdoc />
		public async Task<int> SaveChangesAsync(CancellationToken ct = default)
		{
			return await _db.SaveChangesAsync(ct);
		}

		// <inheritdoc />
		public async Task BeginTransactionAsync(CancellationToken ct = default)
		{
			if (_transaction is not null) return;
			_transaction = await _db.Database.BeginTransactionAsync(ct);
		}

		// <inheritdoc />
		public async Task CommitAsync(CancellationToken ct = default)
		{
			if (_transaction is null) return;
			await _db.SaveChangesAsync(ct);
			await _transaction.CommitAsync(ct);
			await _transaction.DisposeAsync();
			_transaction = null;
		}

		// <inheritdoc />
		public async Task RollbackAsync(CancellationToken ct = default)
		{
			if (_transaction is null) return;
			await _transaction.RollbackAsync(ct);
			await _transaction.DisposeAsync();
			_transaction = null;
		}

		// <inheritdoc />
		public void Dispose()
		{
			_transaction?.Dispose();
			_db.Dispose();
		}
	}
}
