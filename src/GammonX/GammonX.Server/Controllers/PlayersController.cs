using Microsoft.AspNetCore.Mvc;

namespace GammonX.Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class PlayersController : Controller
	{
		[HttpGet]
		public IActionResult Get()
		{
			return Ok();
		}


	}
}
