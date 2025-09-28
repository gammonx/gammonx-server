using GammonX.Engine.History;
using GammonX.Engine.Models;

using System.Text;

namespace GammonX.Server.Models.gameSession
{
	// <inheritdoc />
	internal sealed class GameHistoryImpl : IGameHistory
	{
		// <inheritdoc />
		public Guid Id { get; private set; }

		// <inheritdoc />
		public GameModus Modus { get; private set; }

		// <inheritdoc />
		public int Points { get; private set; }

		// <inheritdoc />
		public Guid WinnerPlayerId { get; private set; }

		// <inheritdoc />
		public DateTime StartedAt { get; private set; }

		// <inheritdoc />
		public DateTime EndedAt { get; private set; }

		// <inheritdoc />
		public IBoardHistory BoardHistory { get; private set; }

		private GameHistoryImpl()
		{
			BoardHistory = BoardHistoryFactory.CreateEmpty();
		}

		public static IGameHistory Create(IGameSessionModel model, Guid winnerPlayerId, int points)
		{
			return new GameHistoryImpl()
			{
				Id = model.Id,
				Modus = model.Modus,
				Points = points,
				WinnerPlayerId = winnerPlayerId,
				StartedAt = model.StartedAt,
				EndedAt = model.EndedAt,
				BoardHistory = model.BoardModel.History
			};
		}

		/// <summary>
		/// Converts this game history into a string representation.
		/// </summary>
		/// <returns>Converted string representation.</returns>
		public override string ToString()
		{
			var stringBuilder = new StringBuilder();
			stringBuilder.AppendLine($";[Game '{Id}']");
			stringBuilder.AppendLine($";[Game Modus '{Modus}']");
			stringBuilder.AppendLine($";[Winner '{WinnerPlayerId}']");
			stringBuilder.AppendLine($";[Points '{Points}']");
			stringBuilder.AppendLine($";[Started At '{StartedAt}']");
			stringBuilder.AppendLine($";[Ended At '{EndedAt}']");
			foreach (var historyEvent in  BoardHistory.Events)
			{
				stringBuilder.AppendLine(historyEvent.ToString());
			}
			return stringBuilder.ToString();
		}
	}
}
