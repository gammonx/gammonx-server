using GammonX.Server.Contracts;
using GammonX.Server.Models;
using GammonX.Server.Services;

using Microsoft.AspNetCore.Mvc;

namespace GammonX.Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class MatchesController : Controller
	{
		private readonly IServiceProvider _serviceProvider;

		public MatchesController(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		[HttpGet]
		public IActionResult Get()
		{
			return Ok();
		}

		[HttpPost("join")]
		public async Task<IActionResult> JoinAsync([FromBody] JoinRequest req)
		{
			try
			{
				var queueKey = new QueueKey(req.MatchVariant, req.MatchModus, req.MatchType);
				var matchMakingService = _serviceProvider.GetRequiredKeyedService<IMatchmakingService>(queueKey.MatchModus);
				var queueEntry = await matchMakingService.JoinQueueAsync(req.PlayerId, queueKey);
				var payload = queueEntry.ToPayload();
				var response = new RequestResponseContract<RequestQueueEntryPayload>("OK", payload);
				return Ok(response);
			}
			catch (Exception e)
			{
				var payload = new RequestErrorPayload("JOIN_ERROR", e.Message);
				var response = new RequestResponseContract<RequestErrorPayload>("ERROR", payload);
				return BadRequest(response);
			}
		}

		[HttpPost("queues/{queueId}")]
		public IActionResult GetStatusAsync([FromRoute] Guid queueId, [FromBody] StatusRequest req)
		{
			try
			{
				var matchMakingService = _serviceProvider.GetRequiredKeyedService<IMatchmakingService>(req.MatchModus);
				if (matchMakingService.TryFindMatchLobby(queueId, out var matchLobby) && matchLobby != null)
				{
					// match lobby was created, return match id
					var payload = matchLobby.ToPayload();
					var response = new RequestResponseContract<RequestQueueEntryPayload>("OK", payload);
					return Ok(response);
				}
				else if (matchMakingService.TryFindQueueEntry(queueId, out var queueEntry) && queueEntry != null)
				{
					// match lobby was not yet created, return queue id
					var payload = queueEntry.ToPayload();
					var response = new RequestResponseContract<RequestQueueEntryPayload>("OK", payload);
					return Ok(response);
				}

				var payloadError = new RequestErrorPayload("QUEUE_ERROR", "No queue entry or match lobby found with the given queue id");
				var responseError = new RequestResponseContract<RequestErrorPayload>("ERROR", payloadError);
				return BadRequest(responseError);
			}
			catch (Exception e)
			{
				var payload = new RequestErrorPayload("QUEUE_ERROR", e.Message);
				var response = new RequestResponseContract<RequestErrorPayload>("ERROR", payload);
				return BadRequest(response);
			}
		}
	}
}
