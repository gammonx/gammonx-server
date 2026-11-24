using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Contracts;
using GammonX.Server.Models;
using GammonX.Server.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GammonX.Server.Controllers
{
    [ApiController]
    [Authorize(Policy = "AuthPolicy")]
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
                var queueKey = new QueueKey(req.MatchVariant, req.MatchModus, req.MatchType, req.BotLevel ?? BotLevel.Unknown);
                var matchMakingService = _serviceProvider.GetRequiredKeyedService<IMatchmakingService>(queueKey.MatchModus);
                // TODO: we have to check if the requesting player is already part of an active match
                // if yes he may corrupt the player connection repository entry
                var queueEntry = await matchMakingService.JoinQueueAsync(req.PlayerId, queueKey);
                
                RequestQueueEntryPayload payload;
                if (req.MatchModus == MatchModus.Bot && matchMakingService.TryFindMatchLobby(queueEntry.Id, out var lobby) && lobby != null)
                {
                    // we can directly return a proper match lobby id for bot modus joins
                    payload = lobby.ToPayload();
                }
                else
                {
                    payload = queueEntry.ToPayload();
                }

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
                if (matchMakingService.TryFindQueueEntry(queueId, out var queueEntry) && 
                    queueEntry != null &&
                    await matchMakingService.LeaveQueueAsync(queueEntry))
                {
                    var payload = queueEntry.ToPayload();
                    payload.Status = QueueEntryStatus.Discarded;
                    var response = new ResponseContract<RequestQueueEntryPayload>("OK", payload);
                    return Ok(response);
                }
                if (matchMakingService.TryFindMatchLobby(queueId, out _))
                {
                    var payloadError = new RequestErrorPayload("QUEUE_ERROR", "Unable to cancel the queue entry. Matchlobby was already created");
                    var responseError = new ResponseContract<RequestErrorPayload>("ERROR", payloadError);
                    return BadRequest(responseError);
                }

                var error = new RequestErrorPayload("QUEUE_ERROR", "No queue entry found with the given queue id");
                return BadRequest(new ResponseContract<RequestErrorPayload>("ERROR", error));
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
                // A missing pending entry can already have a published lobby under the same ID.
                if (matchMakingService.TryFindQueueEntry(queueId, out var queueEntry) && queueEntry != null)
                {
                    // renew TTL
                    matchMakingService.TouchQueueEntry(queueId);
                    // match lobby was not yet created, return queue id
                    var payload = queueEntry.ToPayload();
                    var response = new ResponseContract<RequestQueueEntryPayload>("OK", payload);
                    return Ok(response);
                }

                if (matchMakingService.TryFindMatchLobby(queueId, out var matchLobby) && matchLobby != null)
                {
                    var payload = matchLobby.ToPayload();
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
