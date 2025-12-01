using GammonX.Models.Enums;

using System.Text.RegularExpressions;

namespace GammonX.Models.History.MAT
{
	// <inheritdoc />
	public partial class MATParser : IGameHistoryParser, IMatchHistoryParser
	{
		[GeneratedRegex(@";\[(.+?) '(.+?)'\]", RegexOptions.Compiled)]
		protected partial Regex GameHeaderRegex();

		[GeneratedRegex(@"^(White|Black) Roll ([0-9 ]+)$", RegexOptions.Compiled)]
		protected partial Regex RollRegex();

		[GeneratedRegex(@"^(White|Black) Move ([a-zA-Z0-9]+)/([a-zA-Z0-9]+)$", RegexOptions.Compiled)]
		protected partial Regex MoveRegex();

		// <inheritdoc />
		public IParsedMatchHistory ParseMatch(string content)
		{
			var lines = content.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();

			var matchHistory = new MATMatchHistory();

			int i = 0;
			for (; i < lines.Count; i++)
			{
				if (lines[i].StartsWith(";[Game "))
					break;
				ParseMatchMetadataLine(lines[i], matchHistory);
			}

			List<string> currentGameLines = new();

			for (; i < lines.Count; i++)
			{
				var line = lines[i];
				if (line.StartsWith(";[Game '") && currentGameLines.Count > 0)
				{
					matchHistory.Games.Add(ParseGame(string.Join("\n", currentGameLines)));
					currentGameLines = new List<string>();
				}
				currentGameLines.Add(line);
			}

			// we add the last game
			if (currentGameLines.Count > 0)
				matchHistory.Games.Add(ParseGame(string.Join("\n", currentGameLines)));

			return matchHistory;
		}

		// <inheritdoc />
		public IParsedGameHistory ParseGame(string content)
		{
			var game = new MATGameHistory();

			var lines = content.Split('\n',
				StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

			foreach (var line in lines)
			{
				if (line.StartsWith(";["))
				{
					ParseGameMetadataLine(line, game);
					continue;
				}

				if (TryParseRoll(game, line, out var roll))
				{
					game.Events.Add(roll);
					continue;
				}

				if (TryParseMove(game, line, out var move))
				{
					game.Events.Add(move);
					continue;
				}
			}

			return game;
		}

		private static void ParseMatchMetadataLine(string line, MATMatchHistory match)
		{
			if (!line.StartsWith(";["))
				return;

			var (key, value) = ExtractTag(line);

			switch (key)
			{
				case "Match": match.Id = Guid.Parse(value); break;
				case "Name": match.Name = value; break;
				case "Player 1 White Checkers": match.Player1Id = Guid.Parse(value); break;
				case "Player 2 Black Checkers": match.Player2Id = Guid.Parse(value); break;
				case "Started At": match.StartedAt = DateTime.Parse(value); break;
				case "Ended At": match.EndedAt = DateTime.Parse(value); break;
				case "Length": match.Length = int.Parse(value); break;
			}
		}

		private void ParseGameMetadataLine(string line, MATGameHistory game)
		{
			var (key, value) = ExtractTag(line);

			switch (key)
			{
				case "Game": game.Id = Guid.Parse(value); break;
				case "Game Modus": game.Modus = Enum.Parse<GameModus>(value); break;
				case "Winner": game.Winner = Guid.Parse(value); break;
				case "Points": game.Points = int.Parse(value); break;
				case "Started At": game.StartedAt = DateTime.Parse(value); break;
				case "Ended At": game.EndedAt = DateTime.Parse(value); break;
				case "Player 1 White Checkers": game.Player1Id = Guid.Parse(value); break;
				case "Player 2 Black Checkers": game.Player2Id = Guid.Parse(value); break;
			}
		}

		private bool TryParseRoll(MATGameHistory game, string line, out MatRollEvent roll)
		{
			roll = null!;
			var m = RollRegex().Match(line);
			if (!m.Success) return false;

			var numbers = m.Groups[2].Value
				.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.Select(int.Parse)
				.ToArray();

			string playerColor = m.Groups[1].Value;
			var playerId = MapPlayerId(game, playerColor);

			roll = new MatRollEvent
			{
				PlayerId = playerId,
				Dice = numbers
			};

			return true;
		}

		private bool TryParseMove(MATGameHistory game, string line, out MatMoveEvent move)
		{
			move = null!;
			var m = MoveRegex().Match(line);
			if (!m.Success) return false;

			string playerColor = m.Groups[1].Value;
			var playerId = MapPlayerId(game, playerColor);
			var isWhite = playerId == game.Player1Id;

			move = new MatMoveEvent
			{
				PlayerId = playerId,
				From = ParseFieldIndex(m.Groups[2].Value, isWhite),
				To = ParseFieldIndex(m.Groups[3].Value, isWhite)
			};

			return true;
		}

		private static int ParseFieldIndex(string input, bool isWhite)
		{
			if (int.TryParse(input, out var index))
			{
				return index;
			}
			if (input.Equals("bar"))
			{
				return isWhite ? BoardPositions.HomeBarWhite : BoardPositions.HomeBarBlack;
			}
			if (input.Equals("off"))
			{
				return isWhite ? BoardPositions.BearOffWhite : BoardPositions.BearOffBlack;
			}

			throw new InvalidOperationException($"Unknown field index input of '{input}'");
		}

		private static (string Key, string Value) ExtractTag(string line)
		{
			int firstQuote = line.IndexOf('\'');
			int lastQuote = line.LastIndexOf('\'');

			string value = line.Substring(firstQuote + 1, lastQuote - firstQuote - 1);

			string keyPart = line.Substring(2, firstQuote - 3);

			return (keyPart.Trim(), value.Trim());
		}

		private static Guid MapPlayerId(MATGameHistory game, string color)
		{
			return color switch
			{
				"White" => game.Player1Id,
				"Black" => game.Player2Id,
				_ => throw new InvalidOperationException($"Unknown player color '{color}'")
			};
		}
	}
}
