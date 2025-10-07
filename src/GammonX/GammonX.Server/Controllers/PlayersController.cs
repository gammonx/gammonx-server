using GammonX.Server.Contracts;
using GammonX.Server.Models;
using GammonX.Server.EntityFramework.Services;

using Microsoft.AspNetCore.Mvc;

namespace GammonX.Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class PlayersController : Controller
	{
		private readonly IPlayerService _playerService;

		public PlayersController(IPlayerService playerService)
		{
			_playerService = playerService;
		}

		[HttpGet]
		public IActionResult Get()
		{
			return Ok();
		}

		[HttpPost("create")]
		public async Task<IActionResult> CreateAsync([FromBody] CreateRequest req)
		{
			try
			{
				var playerId = await _playerService.CreateAsync(req.Id, req.UserName);
				var payload = new RequestPlayerIdPayload(playerId);
				var response = new RequestResponseContract<RequestPlayerIdPayload>("OK", payload);
				return Ok(response);
			}
			catch (Exception e)
			{
				var payload = new RequestErrorPayload("CREATE_ERROR", e.Message);
				var response = new RequestResponseContract<RequestErrorPayload>("ERROR", payload);
				return BadRequest(response);
			}
		}

		[HttpPost("{id}/delete")]
		public async Task<IActionResult> DeleteAsync([FromRoute] Guid id)
		{
			try
			{
				var deleted = await _playerService.RemovePlayerAsync(id);
				var payload = new DeleteRequestPayload(deleted);
				var response = new RequestResponseContract<DeleteRequestPayload>("OK", payload);
				return Ok(response);
			}
			catch (Exception e)
			{
				var payload = new RequestErrorPayload("DELETE_ERROR", e.Message);
				var response = new RequestResponseContract<RequestErrorPayload>("ERROR", payload);
				return BadRequest(response);
			}
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetAsync([FromRoute] Guid id)
		{
			try
			{
				var player = await _playerService.GetAsync(id);
				if (player != null)
				{
					var payload = new RequestPlayerPayload(player.ToContract());
					var response = new RequestResponseContract<RequestPlayerPayload>("OK", payload);
					return Ok(response);
				}
				var errorPayload = new RequestErrorPayload("GET_ERROR", "No player with the given id was found.");
				var errorResponse = new RequestResponseContract<RequestErrorPayload>("ERROR", errorPayload);
				return BadRequest(errorResponse);
			}
			catch (Exception e)
			{
				var payload = new RequestErrorPayload("GET_ERROR", e.Message);
				var response = new RequestResponseContract<RequestErrorPayload>("ERROR", payload);
				return BadRequest(response);
			}
		}
	}
}
