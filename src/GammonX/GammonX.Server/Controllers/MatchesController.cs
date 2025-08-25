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
		private readonly SimpleMatchmakingService _service;

		public MatchesController(SimpleMatchmakingService service)
		{
			_service = service;
		}

		[HttpGet]
		public IActionResult Get()
		{
			return Ok();
		}

		[HttpPost("join")]
		public IActionResult Join([FromBody] JoinRequest req)
		{
			try
			{
				var player = new LobbyEntry(req.PlayerId);
				var matchId = _service.JoinQueue(player, req.MatchVariant);
				var payload = new RequestMatchIdPayload(matchId);
				var response = new RequestResponseContract<RequestMatchIdPayload>("OK", payload);
				return Ok(response);
			}
			catch (Exception e)
			{
				var payload = new RequestErrorPayload("JOIN_ERROR", e.Message);
				var response = new RequestResponseContract<RequestErrorPayload>("ERROR", payload);
				return BadRequest(response);
			}
		}
	}
}
