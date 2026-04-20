using GammonX.Models.Contracts;

using GammonX.Server.Contracts;
using GammonX.Server.Models;
using GammonX.Server.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GammonX.Server.Controllers
{
    [ApiController]
    [Authorize(Policy = "OptionalJwt")]
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
                var response = new ResponseContract<RequestQueueEntryPayload>("OK", payload);
                return Ok(response);
            }
            catch (Exception e)
            {
                var payload = new RequestErrorPayload("JOIN_ERROR", e.Message);
                var response = new ResponseContract<RequestErrorPayload>("ERROR", payload);
                return BadRequest(response);
            }
        }

        [HttpPost("queues/{queueId}/cancel")]
        public async Task<IActionResult> CancelAsync([FromRoute] Guid queueId, [FromBody] StatusRequest req)
        {
            try
            {
                var matchMakingService = _serviceProvider.GetRequiredKeyedService<IMatchmakingService>(req.MatchModus);
                if (matchMakingService.TryFindMatchLobby(queueId, out var matchLobby) && matchLobby != null)
                {
                    // match lobby was created, return match id
                    var payload = matchLobby.ToPayload();
                    var payloadError = new RequestErrorPayload("QUEUE_ERROR", "Unable to cancel the queue entry. Matchlobby was already created");
                    var responseError = new ResponseContract<RequestErrorPayload>("ERROR", payloadError);
                    return BadRequest(responseError);
                }
                else if (matchMakingService.TryFindQueueEntry(queueId, out var queueEntry) && queueEntry != null)
                {
                    await matchMakingService.LeaveQueueAsync(queueEntry);
                    var payload = queueEntry.ToPayload();
                    payload.Status = QueueEntryStatus.Discarded;
                    var response = new ResponseContract<RequestQueueEntryPayload>("OK", payload);
                    return Ok(response);
                }
                else
                {
                    var payloadError = new RequestErrorPayload("QUEUE_ERROR", "No queue entry found with the given queue id");
                    var responseError = new ResponseContract<RequestErrorPayload>("ERROR", payloadError);
                    return BadRequest(responseError);
                }                
            }
            catch (Exception e)
            {
                var payload = new RequestErrorPayload("QUEUE_ERROR", e.Message);
                var response = new ResponseContract<RequestErrorPayload>("ERROR", payload);
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
                    var response = new ResponseContract<RequestQueueEntryPayload>("OK", payload);
                    return Ok(response);
                }
                else if (matchMakingService.TryFindQueueEntry(queueId, out var queueEntry) && queueEntry != null)
                {
                    // renew TTL
                    matchMakingService.TouchQueueEntry(queueId);
                    // match lobby was not yet created, return queue id
                    var payload = queueEntry.ToPayload();
                    var response = new ResponseContract<RequestQueueEntryPayload>("OK", payload);
                    return Ok(response);
                }

                var payloadError = new RequestErrorPayload("QUEUE_ERROR", "No queue entry or match lobby found with the given queue id");
                var responseError = new ResponseContract<RequestErrorPayload>("ERROR", payloadError);
                return BadRequest(responseError);
            }
            catch (Exception e)
            {
                var payload = new RequestErrorPayload("QUEUE_ERROR", e.Message);
                var response = new ResponseContract<RequestErrorPayload>("ERROR", payload);
                return BadRequest(response);
            }
        }
    }
}
