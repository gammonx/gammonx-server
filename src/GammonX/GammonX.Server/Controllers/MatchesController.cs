using GammonX.Server.Models;
using GammonX.Server.Services;

using Microsoft.AspNetCore.Mvc;

namespace GammonX.Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class MatchesController : Controller
	{
		private readonly MatchmakingService _service;

		public MatchesController(MatchmakingService service)
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
				var player = new Player(req.ClientId);
				var matchId = _service.JoinQueue(player, req.MatchVariant);
				return Ok(new
				{
					type = "message",
					message = "Joined matchmaking queue, waiting for opponent to start the game.",
					data = matchId
				});
			}
			catch (Exception e)
			{
				return BadRequest(new
				{
					type = "error",
					code = "LOBBY_ERROR",
					message = e.Message,
					data = e
				});
			}
		}
	}
}
