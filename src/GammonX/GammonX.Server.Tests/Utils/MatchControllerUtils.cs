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
			try
			{
                var req = new StatusRequest(playerId, modus);
                var response = await client.PostAsJsonAsync($"/api/matches/queues/{queueId}", req);
                var statusJson = await response.Content.ReadAsStringAsync();
                Assert.NotNull(statusJson);
                var status = JsonConvert.DeserializeObject<RequestResponseContract<RequestQueueEntryPayload>>(statusJson);
                Assert.NotNull(status);
                return status.Payload;
            }
            catch (Exception)
            {
                //  sometimes the timing is a bit off with the integration tests, so we just retry once
                return new RequestQueueEntryPayload() { MatchId = null, QueueId = queueId, Status = QueueEntryStatus.WaitingForOpponent};
            }
        }
	}
}
