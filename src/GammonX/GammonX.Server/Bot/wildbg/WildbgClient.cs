using Newtonsoft.Json;

using System.Text;

namespace GammonX.Server.Bot
{
	public class WildbgClient
	{
		private readonly HttpClient _httpClient;

		public WildbgClient(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		/// <summary>
		/// Moves for position/dice. Returns a list of legal moves for a certain position and pair of dice, ordered by match equity.
		/// </summary>
		public async Task<GetMoveResponse> GetMoveAsync(GetMoveParameter request)
		{
			if (request.Points.Count == 0)
				throw new ArgumentException("Points must contain at least one occupied point.", nameof(request));

			var qs = new StringBuilder();
			void Add(string key, object value)
			{
				if (qs.Length > 0) qs.Append('&');
				qs.Append(Uri.EscapeDataString(key));
				qs.Append('=');
				qs.Append(Uri.EscapeDataString(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!));
			}

			Add("die1", request.DiceRoll1);
			Add("die2", request.DiceRoll2);
			Add("x_away", request.XPointsAway);
			Add("o_away", request.OPointsAway);

			foreach (var kv in request.Points)
			{
				var point = kv.Key;
				var count = kv.Value;
				if (point < 0 || point > 25)
					throw new ArgumentOutOfRangeException(nameof(request.Points), $"Point {point} is out of range (0..25).");
				if (count != 0)
					Add($"p{point}", count);
			}

			var uri = new Uri($"/move?{qs}", UriKind.Relative);

			using var resp = await _httpClient.GetAsync(uri);
			resp.EnsureSuccessStatusCode();

			var response = await resp.Content.ReadAsStringAsync();
			var moveResponse = JsonConvert.DeserializeObject<GetMoveResponse>(response);

			if (moveResponse == null)
				throw new BadHttpRequestException(response);

			return moveResponse;
		}
	}
}
