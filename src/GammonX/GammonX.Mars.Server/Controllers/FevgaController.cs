using GammonX.Mars.Server.Contracts;

using Microsoft.AspNetCore.Mvc;

namespace GammonX.Mars.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FevgaController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }

        [HttpPost("eval")]
        public IActionResult Eval()
        {
            // TODO: evaulate the state of the board
            return Ok();
        }

        [HttpPost("move")]
        public IActionResult Move([FromBody] MoveRequestContract request)
        {
            // TODO: calculate the best move for the given board state and dice roll
            return Ok();
        }
    }
}
