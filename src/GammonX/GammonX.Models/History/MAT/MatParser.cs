using System.Text.RegularExpressions;

namespace GammonX.Models.History.MAT
{
	// <inheritdoc />
	public partial class MatParser : IGameHistoryParser
	{
		// TODO: add unit tests

		[GeneratedRegex(@";\[(.+?) '(.+?)'\]", RegexOptions.Compiled)]
		protected partial Regex HeaderRegex();

		[GeneratedRegex(@"^(White|Black) Roll ([0-9 ]+)$", RegexOptions.Compiled)]
		protected partial Regex RollRegex();

		[GeneratedRegex(@"^(White|Black) Move ([a-zA-Z0-9]+)/([a-zA-Z0-9]+)$", RegexOptions.Compiled)]
		protected partial Regex MoveRegex();

		// <inheritdoc />
		public IParsedGameHistory Parse(string content)
		{
			var game = new MatGameHistory();

			var lines = content.Split('\n',
				StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

			foreach (var line in lines)
			{
				if (line.StartsWith(";["))
				{
					ParseHeader(game, line);
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

		private void ParseHeader(MatGameHistory game, string line)
		{
			var m = HeaderRegex().Match(line);
			if (!m.Success) return;

			string key = m.Groups[1].Value;
			string value = m.Groups[2].Value;

			game.Headers.Add(new MatHeaderEntry { Key = key, Value = value });

			if (key.Contains("White Checkers"))
				game.PlayerWhiteId = value;

			if (key.Contains("Black Checkers"))
				game.PlayerBlackId = value;
		}

		private bool TryParseRoll(MatGameHistory game, string line, out MatRollEvent roll)
		{
			roll = null!;
			var m = RollRegex().Match(line);
			if (!m.Success) return false;

			var numbers = m.Groups[2].Value
				.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.Select(int.Parse)
				.ToArray();

			string playerColor = m.Groups[1].Value;
			string playerId = MapPlayer(game, playerColor);

			roll = new MatRollEvent
			{
				Player = playerId,
				Dice = numbers
			};

			return true;
		}

		private bool TryParseMove(MatGameHistory game, string line, out MatMoveEvent move)
		{
			move = null!;
			var m = MoveRegex().Match(line);
			if (!m.Success) return false;

			string playerColor = m.Groups[1].Value;
			string playerId = MapPlayer(game, playerColor);

			move = new MatMoveEvent
			{
				Player = playerId,
				From = m.Groups[2].Value,
				To = m.Groups[3].Value
			};

			return true;
		}

		private string MapPlayer(MatGameHistory game, string color)
		{
			return color switch
			{
				"White" => game.PlayerWhiteId,
				"Black" => game.PlayerBlackId,
				_ => throw new InvalidOperationException($"Unknown player color '{color}'")
			};
		}
	}
}
