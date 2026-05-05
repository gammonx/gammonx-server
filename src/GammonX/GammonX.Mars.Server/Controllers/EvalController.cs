using GammonX.Engine.Models;

using GammonX.Mars.NN;
using GammonX.Mars.NN.Services;

using GammonX.Mars.Server.Contracts;

using GammonX.Models.Contracts;

using Microsoft.AspNetCore.Mvc;

namespace GammonX.Mars.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EvalController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;

        public EvalController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }

        [HttpPost("board")]
        public IActionResult Board([FromBody] EvalBoardRequestContract request)
        {
            try
            {
                var evalService = _serviceProvider.GetRequiredKeyedService<IFeatureEvalService>(request.Modus);

                // TODO: weights are obsolete with a loaded neural network

                EvalWeights.RaceWeights.Validate();
                EvalWeights.PlakotoContactWeights.Validate();
                EvalWeights.PlakotoCheapContactWeights.Validate();

                var boardScore = evalService.EvalBoardState(request, EvalWeights.PlakotoCheapContactWeights, EvalWeights.PlakotoContactWeights, EvalWeights.RaceWeights);
                var payload = new BoardEvalPayload { EvalScore = boardScore };
                var response = new ResponseContract<BoardEvalPayload>("OK", payload);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var payload = new RequestErrorPayload("BOARD_EVAL_ERROR", ex.Message);
                var response = new ResponseContract<RequestErrorPayload>("ERROR", payload);
                return BadRequest(response);
            }
        }

        [HttpPost("move")]
        public IActionResult Move([FromBody] EvalMoveRequestContract request)
        {
            try
            {
                var evalService = _serviceProvider.GetRequiredKeyedService<IFeatureEvalService>(request.Modus);

                EvalWeights.RaceWeights.Validate();
                EvalWeights.PlakotoContactWeights.Validate();
                EvalWeights.PlakotoCheapContactWeights.Validate();

                var bestMove = evalService.EvalMoveSequence(request, EvalWeights.PlakotoCheapContactWeights, EvalWeights.PlakotoContactWeights, EvalWeights.RaceWeights, 20);
                if (bestMove != null)
                {
                    var payload = new MoveEvalPayload() { MoveSequence = bestMove };
                    var response = new ResponseContract<MoveEvalPayload>("OK", payload);
                    return Ok(response);
                }
                else
                {
                    var payload = new MoveEvalPayload() { MoveSequence = new MoveSequenceModel() };
                    var response = new ResponseContract<MoveEvalPayload>("OK", payload);
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                var payload = new RequestErrorPayload("MOVE_EVAL_ERROR", ex.Message);
                var response = new ResponseContract<RequestErrorPayload>("ERROR", payload);
                return BadRequest(response);
            }
        }
    }
}
