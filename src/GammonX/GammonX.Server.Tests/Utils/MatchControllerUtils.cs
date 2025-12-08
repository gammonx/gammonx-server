using GammonX.Models.Enums;

using GammonX.Server.Contracts;
using GammonX.Server.Models;

using Newtonsoft.Json;

using System.Net.Http.Json;

namespace GammonX.Server.Tests.Utils
{
	public static class MatchControllerUtils
	{
		public static async Task<RequestQueueEntryPayload> PollAsync(this HttpClient client, Guid playerId, Guid queueId, MatchModus modus)
		{
			var req = new StatusRequest(playerId, modus);
			var response = await client.PostAsJsonAsync($"/api/matches/queues/{queueId}", req);
			var statusJson = await response.Content.ReadAsStringAsync();
			Assert.NotNull(statusJson);
			var status = JsonConvert.DeserializeObject<RequestResponseContract<RequestQueueEntryPayload>>(statusJson);
			Assert.NotNull(status);
			return status.Payload;
		}
	}
}
