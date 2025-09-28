using System.Text;

namespace GammonX.Server.Models
{
	// <inheritdoc />
	public sealed class MatchHistoryImpl : IMatchHistory
	{
		// <inheritdoc />
		public Guid Id { get; private set; }

		// <inheritdoc />
		public string Name { get; private set; }

		// <inheritdoc />
		public Guid Player1 { get; private set; }

		// <inheritdoc />
		public Guid Player2 { get; private set; }

		// <inheritdoc />
		public DateTime StartedAt { get; private set; }

		// <inheritdoc />
		public DateTime EndedAt { get; private set; }

		// <inheritdoc />
		public int Length { get; private set; }

		// <inheritdoc />
		public IGameHistory[] Games { get; private set; }

		private MatchHistoryImpl()
		{
			Name = string.Empty;
			Games = Array.Empty<IGameHistory>();
		}

		public static IMatchHistory Create(IMatchSessionModel model)
		{
			var playedGames = model.GetGameSessions().Where(gs => gs != null && gs.Phase == GamePhase.GameOver);
			return new MatchHistoryImpl()
			{
				Id = model.Id,
				Name = ConstructMatchName(model),
				Player1 = model.Player1.Id,
				Player2 = model.Player2.Id,
				StartedAt = model.StartedAt,
				EndedAt = model.EndedAt ?? DateTime.UtcNow,
				Length = playedGames.Count(),
				Games = playedGames.Select(gs => gs.GetHistory()).ToArray()
			};

		}

		/// <summary>
		/// Converts this match history into a string representation.
		/// </summary>
		/// <returns>Converted string representation.</returns>
		public override string ToString()
		{
			var stringBuilder = new StringBuilder();
			stringBuilder.AppendLine($";[Match '{Id}']");
			stringBuilder.AppendLine($";[Name '{Name}']");
			stringBuilder.AppendLine($";[Player 1 White Checkers '{Player1}']");
			stringBuilder.AppendLine($";[Player 2 Black Checkers '{Player2}']");
			stringBuilder.AppendLine($";[Started At '{StartedAt}']");
			stringBuilder.AppendLine($";[Ended At '{EndedAt}']");
			stringBuilder.AppendLine($";[Length '{Length}']");
			foreach (var game in Games)
			{
				stringBuilder.AppendLine(game.ToString());
			}
			return stringBuilder.ToString();
		}

		private static string ConstructMatchName(IMatchSessionModel model)
		{
			return $"{model.Variant} {model.Modus} {model.Type}";
		}
	}
}
