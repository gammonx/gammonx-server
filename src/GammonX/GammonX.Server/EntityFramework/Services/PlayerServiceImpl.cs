using GammonX.Server.EntityFramework.Entities;

namespace GammonX.Server.EntityFramework.Services
{
	// <inheritdoc />
	internal sealed class PlayerServiceImpl : IPlayerService
	{
		private readonly IUnitOfWork _unitOfWork;

		public PlayerServiceImpl(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		// <inheritdoc />
		public async Task<Player?> GetPlayerAsync(Guid id, CancellationToken ct = default)
		{
			return await _unitOfWork.Players.GetByIdAsync(id, ct);
		}

		// <inheritdoc />
		public async Task<Guid> CreatePlayerAsync(Guid id, CancellationToken ct = default)
		{
			var existing = await _unitOfWork.Players.GetByIdAsync(id, ct);
			if (existing is not null)
				throw new InvalidOperationException("A player with the given id already exists.");


			var player = new Player { Id = id };
			await _unitOfWork.Players.AddAsync(player, ct);
			await _unitOfWork.SaveChangesAsync(ct);
			return player.Id;
		}
	}
}
