using GammonX.Engine.History;

using GammonX.Models.Enums;

using System.Text;

namespace GammonX.Server.Models.gameSession
{
	// <inheritdoc />
	internal sealed class GameHistoryImpl : IGameHistory
	{
		// <inheritdoc />
		public Guid Id { get; private set; }

		// <inheritdoc />
		public Guid Player1 { get; private set; }

		// <inheritdoc />
		public Guid Player2 { get; private set; }

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

		// <inheritdoc />
		public HistoryFormat Format => HistoryFormat.MAT;

		private GameHistoryImpl()
		{
			BoardHistory = BoardHistoryFactory.CreateEmpty();
		}

		public static IGameHistory Create(
			IGameSessionModel model, 
			Guid player1,
			Guid player2,
			Guid winnerPlayerId, 
			int points)
		{
			return new GameHistoryImpl()
			{
				Id = model.Id,
				Player1 = player1,
				Player2 = player2,
				Modus = (GameModus)model.Modus,
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
			stringBuilder.AppendLine($";[Player 1 White Checkers '{Player1}']");
			stringBuilder.AppendLine($";[Player 2 Black Checkers '{Player2}']");
			stringBuilder.AppendLine($";[Game Modus '{Modus}']");
			stringBuilder.AppendLine($";[Winner '{WinnerPlayerId}']");
			stringBuilder.AppendLine($";[Points '{Points}']");
			stringBuilder.AppendLine($";[Started At '{StartedAt}']");
			stringBuilder.AppendLine($";[Ended At '{EndedAt}']");
			foreach (var historyEvent in  BoardHistory.Events.Reverse())
			{
				stringBuilder.AppendLine(historyEvent.ToString());
			}
			return stringBuilder.ToString();
		}
	}
}
