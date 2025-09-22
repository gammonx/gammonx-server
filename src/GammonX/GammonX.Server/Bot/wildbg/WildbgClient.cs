using Newtonsoft.Json;

using System.Text;

namespace GammonX.Server.Bot
{
	/// <summary>
	/// Integration client for open source wildbg bot.
	/// </summary>
	/// <seealso cref="https://github.com/carsten-wenderdel/wildbg"/>
	public class WildbgClient
	{
		private readonly HttpClient _httpClient;

		public WildbgClient(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		/// <summary>
		/// Returns probabilities and cube decisions for a given position.
		/// </summary>
		/// <param name="parameters">Request parameters.</param>
		/// <returns></returns>
		public async Task<GetEvalResponse> GetEvalAsync(GetEvalParameter parameters)
		{
			if (parameters.Points.Count == 0)
				throw new ArgumentException("Points must contain at least one occupied point.", nameof(parameters));

			var sb = new StringBuilder();

			foreach (var kv in parameters.Points)
			{
				var point = kv.Key;
				var count = kv.Value;
				if (point < 0 || point > 25)
					throw new ArgumentOutOfRangeException(nameof(parameters), $"Point {point} is out of range (0..25).");
				if (count != 0)
					Add(sb, $"p{point}", count);
			}

			var uri = new Uri($"/eval?{sb}", UriKind.Relative);

			using var resp = await _httpClient.GetAsync(uri);
			resp.EnsureSuccessStatusCode();

			var response = await resp.Content.ReadAsStringAsync();
			var moveResponse = JsonConvert.DeserializeObject<GetEvalResponse>(response);

			if (moveResponse == null)
				throw new BadHttpRequestException(response);

			return moveResponse;
		}

		/// <summary>
		/// Moves for position/dice. Returns a list of legal moves for a certain position and pair of dice, ordered by match equity.
		/// </summary>
		/// <param name="parameters">Request parameters.</param>
		/// <returns>An intance of <see cref="GetMoveResponse"/>.</returns>
		public async Task<GetMoveResponse> GetMoveAsync(GetMoveParameter parameters)
		{
			if (parameters.Points.Count == 0)
				throw new ArgumentException("Points must contain at least one occupied point.", nameof(parameters));

			var sb = new StringBuilder();

			Add(sb, "die1", parameters.DiceRoll1);
			Add(sb, "die2", parameters.DiceRoll2);
			Add(sb, "x_away", parameters.XPointsAway);
			Add(sb, "o_away", parameters.OPointsAway);

			foreach (var kv in parameters.Points)
			{
				var point = kv.Key;
				var count = kv.Value;
				if (point < 0 || point > 25)
					throw new ArgumentOutOfRangeException(nameof(parameters), $"Point {point} is out of range (0..25).");
				if (count != 0)
					Add(sb, $"p{point}", count);
			}

			var uri = new Uri($"/move?{sb}", UriKind.Relative);

			using var resp = await _httpClient.GetAsync(uri);
			resp.EnsureSuccessStatusCode();

			var response = await resp.Content.ReadAsStringAsync();
			var moveResponse = JsonConvert.DeserializeObject<GetMoveResponse>(response);

			if (moveResponse == null)
				throw new BadHttpRequestException(response);

			return moveResponse;
		}

		private static void Add(StringBuilder sb, string key, object value)
		{
			if (sb.Length > 0) sb.Append('&');
			sb.Append(Uri.EscapeDataString(key));
			sb.Append('=');
			sb.Append(Uri.EscapeDataString(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!));
		}
	}
}
