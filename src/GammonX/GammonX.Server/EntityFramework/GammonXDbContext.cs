using GammonX.Server.EntityFramework.Entities;

using Microsoft.EntityFrameworkCore;

namespace GammonX.Server.EntityFramework
{
	// <inheritdoc />
	public class GammonXDbContext : DbContext
	{
		/// <summary>
		/// Gets the db set of the player entity.
		/// </summary>
		public DbSet<Player> Players => Set<Player>();

		/// <summary>
		/// Gets the db set of the match entity.
		/// </summary>
		public DbSet<Match> Matches => Set<Match>();

		/// <summary>
		/// Gets the db set of the game entity.
		/// </summary>
		public DbSet<Game> Games => Set<Game>();

		public GammonXDbContext(DbContextOptions<GammonXDbContext> options) : base(options)
		{

		}

		// <inheritdoc />
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			#region Create Tables

			modelBuilder.Entity<Player>()
				.HasIndex(p => p.Id)
				.IsUnique();

			modelBuilder.Entity<Match>()
				.HasIndex(m => m.Id)
				.IsUnique();

			modelBuilder.Entity<MatchHistory>()
				.HasIndex(m => m.MatchId)
				.IsUnique();

			modelBuilder.Entity<GameHistory>()
				.HasIndex(g => g.GameId)
				.IsUnique();

			modelBuilder.Entity<Game>()
				.HasIndex(g => g.Id)
				.IsUnique();

			modelBuilder.Entity<PlayerRating>()
				.HasIndex(r => new { r.PlayerId, r.Variant, r.Type })
				.IsUnique();

			modelBuilder.Entity<PlayerStats>()
				.HasIndex(s => new { s.PlayerId, s.Variant, s.Type, s.Modus })
				.IsUnique();

			#endregion Create Tables

			#region Create Relations

			// match > winner (1:1)
			modelBuilder.Entity<Match>()
				.HasOne(m => m.Winner)
				.WithMany(p => p.WonMatches)
				.HasForeignKey(m => m.WinnerId)
				.OnDelete(DeleteBehavior.Restrict);

			// match > loser (1:1)
			modelBuilder.Entity<Match>()
				.HasOne(m => m.Loser)
				.WithMany(p => p.LostMatches)
				.HasForeignKey(m => m.LoserId)
				.OnDelete(DeleteBehavior.Restrict);

			// match > games (1:n)
			modelBuilder.Entity<Match>()
				.HasMany(m => m.Games)
				.WithOne(g => g.Match)
				.HasForeignKey(g => g.MatchId)
				.OnDelete(DeleteBehavior.Cascade);

			// game > winner (n:1)
			modelBuilder.Entity<Game>()
				.HasOne(g => g.Winner)
				.WithMany(p => p.GamesWon)
				.HasForeignKey(g => g.WinnerId)
				.OnDelete(DeleteBehavior.Restrict);

			// game > winner (n:1)
			modelBuilder.Entity<Game>()
				.HasOne(g => g.Loser)
				.WithMany(p => p.GamesLost)
				.HasForeignKey(g => g.LoserId)
				.OnDelete(DeleteBehavior.Restrict);

			// match > match history (1:1)
			modelBuilder.Entity<Match>()
				.HasOne(m => m.History)
				.WithOne(h => h.Match)
				.HasForeignKey<MatchHistory>(h => h.MatchId)
				.OnDelete(DeleteBehavior.Cascade);

			// game > game history (1:1)
			modelBuilder.Entity<Game>()
				.HasOne(g => g.History)
				.WithOne(h => h.Game)
				.HasForeignKey<GameHistory>(h => h.GameId)
				.OnDelete(DeleteBehavior.Cascade);

			// player > rating (1:n)
			modelBuilder.Entity<PlayerRating>()
				.HasOne(r => r.Player)
				.WithMany(p => p.Ratings)
				.HasForeignKey(r => r.PlayerId)
				.OnDelete(DeleteBehavior.Cascade);

			// player > stats (1:n)
			modelBuilder.Entity<PlayerStats>()
				.HasOne(s => s.Player)
				.WithMany(p => p.Stats)
				.HasForeignKey(s => s.PlayerId)
				.OnDelete(DeleteBehavior.Cascade);

			#endregion Create Relations
		}
	}
}
