using GammonX.Server.Models;
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
		public async Task<Player?> GetAsync(Guid id, CancellationToken ct = default)
		{
			return await _unitOfWork.Players.GetByIdAsync(id, ct);
		}

		// <inheritdoc />
		public async Task<Player?> GetWithRatingAsync(Guid id, CancellationToken ct = default)
		{
			return await _unitOfWork.Players.GetWithRatingAsync(id, ct);
		}

		// <inheritdoc />
		public async Task<Player?> GetFull(Guid id, CancellationToken ct = default)
		{
			return await _unitOfWork.Players.GetFullPlayerAsync(id, ct);
		}

		// <inheritdoc />
		public async Task<Guid> CreateAsync(Guid id, string userName, CancellationToken ct = default)
		{
			var existing = await _unitOfWork.Players.GetByIdAsync(id, ct);
			if (existing != null)
				throw new InvalidOperationException("A player with the given id already exists.");

			var player = new Player
			{
				Id = id,
				UserName = userName,
				GamesLost = Array.Empty<Game>(),
				GamesWon = Array.Empty<Game>(),
				LostMatches = Array.Empty<Match>(),
				WonMatches = Array.Empty<Match>()
			};
			player.Ratings = CreateInitalPlayerRatings(player).ToArray();
			player.Stats = CreateInitialPlayerStats(player).ToArray();

			await _unitOfWork.Players.AddAsync(player, ct);
			await _unitOfWork.SaveChangesAsync(ct);
			return player.Id;
		}

		// <inheritdoc />
		public async Task UpdateAsync(Player player, CancellationToken ct = default)
		{
			var existing = await _unitOfWork.Players.GetByIdAsync(player.Id, ct);
			if (existing == null)
				throw new InvalidOperationException("No player with the given id exists.");

			_unitOfWork.Players.Update(player);
			await _unitOfWork.SaveChangesAsync(ct);
		}

		// <inheritdoc />
		public async Task<bool> RemovePlayerAsync(Guid id, CancellationToken ct = default)
		{
			var player = await _unitOfWork.Players.GetByIdAsync(id, ct);
			if (player != null)
			{
				_unitOfWork.Players.Remove(player);
				return true;
			}
			else
			{
				return false;
			}
		}

		private static List<PlayerRating> CreateInitalPlayerRatings(Player player)
		{
			var playerRatings = new List<PlayerRating>();
			foreach (var variant in Enum.GetValues(typeof(WellKnownMatchVariant)))
			{
				var playerRating = new PlayerRating
				{
					Id = Guid.NewGuid(),
					Rating = 1200,
					LowestRating = 1200,
					HighestRating = 1200,
					MatchesPlayed = 0,
					Variant = (WellKnownMatchVariant)variant,
					// we only support sevent point games for rankeds atm
					Type = WellKnownMatchType.SevenPointGame,
					Player = player,
					PlayerId = player.Id
				};
				playerRatings.Add(playerRating);
			}
			return playerRatings;
		}

		private static List<PlayerStats> CreateInitialPlayerStats(Player player)
		{
			var playerStats = new List<PlayerStats>();
			foreach (var variant in Enum.GetValues(typeof(WellKnownMatchVariant)))
			{
				foreach (var modus in Enum.GetValues(typeof(WellKnownMatchModus)))
				{
					foreach (var type in Enum.GetValues(typeof (WellKnownMatchType)))
					{
						var playerStat = new PlayerStats
						{
							Id = Guid.NewGuid(),
							Player = player,
							PlayerId = player.Id,
							Variant = (WellKnownMatchVariant)variant,
							Modus = (WellKnownMatchModus)modus,
							Type = (WellKnownMatchType)type,
							AverageMatchLengthInMs = 0,
							TotalPlayTimeInMs = 0,
							LastMatch = DateTime.MinValue,
							MatchesLast30Days = 0,
							MatchesLast7Days = 0,
							LongestWinStreak = 0,
							WinStreak = 0,
							MatchesLost = 0,
							MatchesWon = 0,
							MatchesPlayed = 0,
							WinRate = 0.0
						};
						playerStats.Add(playerStat);
					}
				}
			}
			return playerStats;
		}
	}
}
