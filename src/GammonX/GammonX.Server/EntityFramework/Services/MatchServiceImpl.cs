using GammonX.Server.EntityFramework.Entities;

namespace GammonX.Server.EntityFramework.Services
{
	// <inheritdoc />
	internal sealed class MatchServiceImpl : IMatchService
	{
		private readonly IUnitOfWork _unitOfWork;

		public MatchServiceImpl(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		// <inheritdoc />
		public async Task<Match?> GetMatchAsync(Guid id, CancellationToken ct = default)
		{
			return await _unitOfWork.Matches.GetByIdAsync(id, ct);
		}

		// <inheritdoc />
		public async Task<Guid> CreateMatchAsync(Guid id, CancellationToken ct = default)
		{
			var existing = await _unitOfWork.Matches.GetByIdAsync(id, ct);
			if (existing is not null)
				throw new InvalidOperationException("A match with the given id already exists.");


			var match = new Match { Id = id };
			await _unitOfWork.Matches.AddAsync(match, ct);
			await _unitOfWork.SaveChangesAsync(ct);
			return match.Id;
		}
	}
}
