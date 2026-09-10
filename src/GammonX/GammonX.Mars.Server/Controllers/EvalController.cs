using GammonX.Engine.Models;

using GammonX.Mars.NN;
using GammonX.Mars.NN.Services;

using GammonX.Mars.Server.Contracts;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

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

        [HttpPost("cube")]
        public async Task<IActionResult> Cube([FromBody] EvalCubeRequestContract request)
        {
            try
            {
                if (request.BotLevel == BotLevel.Unknown)
                {
                    throw new ArgumentException("BotLevel cannot be Unknown.", nameof(request));
                }

                var evalService = _serviceProvider.GetRequiredKeyedService<IFeatureEvalService>(request.Modus);

                var raceWeights = EvalWeights.GetRaceWeights(request.Modus);
                var contactWeights = EvalWeights.GetContactWeights(request.Modus);
                var cheapContactWeights = EvalWeights.GetCheapContactWeights(request.Modus);

                raceWeights.Validate();
                contactWeights.Validate();
                cheapContactWeights.Validate();

                var (shouldOffer, shouldTake) = await evalService.EvalCubeAsync(request);
                var payload = new CubeEvalPayload { ShouldOffer = shouldOffer, ShouldTake = shouldTake };
                var response = new ResponseContract<CubeEvalPayload>("OK", payload);
                return Ok(response);
            }
            catch (Exception ex) 
            {
                var payload = new RequestErrorPayload("CUBE_EVAL_ERROR", ex.Message);
                var response = new ResponseContract<RequestErrorPayload>("ERROR", payload);
                return BadRequest(response);
            }
        }

        [HttpPost("board")]
        public async Task<IActionResult> Board([FromBody] EvalBoardRequestContract request)
        {
            try
            {
                var evalService = _serviceProvider.GetRequiredKeyedService<IFeatureEvalService>(request.Modus);
                var contactWeights = EvalWeights.GetContactWeights(request.Modus);
                contactWeights.Validate();
                var boardScore = await evalService.EvalBoardStateAsync(request, contactWeights);
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
        public async Task<IActionResult> Move([FromBody] EvalMoveRequestContract request)
        {
            try
            {
                if (request.BotLevel == BotLevel.Unknown)
                {
                    throw new ArgumentException("BotLevel cannot be Unknown.", nameof(request));
                }

                var evalService = _serviceProvider.GetRequiredKeyedService<IFeatureEvalService>(request.Modus);

                var contactWeights = EvalWeights.GetContactWeights(request.Modus);

                contactWeights.Validate();

                var bestMove = await evalService.EvalMoveSequencesAsync(request, contactWeights);
                if (bestMove != null)
                {
                    var payload = new MoveEvalPayload { MoveSequence = bestMove };
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
