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
        public async Task<IActionResult> Cube([FromBody] EvalCubeRequestContract request, CancellationToken cancellationToken)
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

                var options = new FeatureEvalOptions
                {
                    EvalCubeRequest = request,
                    TurnNumber = request.TurnNumber,
                    IsWhite = request.IsWhite,
                };

                var (shouldOffer, shouldTake) = await evalService.EvalCubeAsync(options, cancellationToken);
                var payload = new CubeEvalPayload { ShouldOffer = shouldOffer, ShouldTake = shouldTake };
                var response = new ResponseContract<CubeEvalPayload>("OK", payload);
                return Ok(response);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) 
            {
                var payload = new RequestErrorPayload("CUBE_EVAL_ERROR", ex.Message);
                var response = new ResponseContract<RequestErrorPayload>("ERROR", payload);
                return BadRequest(response);
            }
        }

        [HttpPost("board")]
        public async Task<IActionResult> Board([FromBody] EvalBoardRequestContract request, CancellationToken cancellationToken)
        {
            try
            {
                var evalService = _serviceProvider.GetRequiredKeyedService<IFeatureEvalService>(request.Modus);
                var contactWeights = EvalWeights.GetContactWeights(request.Modus);
                contactWeights.Validate();

                var options = new FeatureEvalOptions
                {
                    EvalBoardRequest = request,
                    TurnNumber = request.TurnNumber,
                    IsWhite = request.IsWhite,
                    ContactWeights = contactWeights,
                };

                var boardScore = await evalService.EvalBoardStateAsync(options, cancellationToken);
                var payload = new BoardEvalPayload { EvalScore = boardScore.Equity };
                var response = new ResponseContract<BoardEvalPayload>("OK", payload);
                return Ok(response);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var payload = new RequestErrorPayload("BOARD_EVAL_ERROR", ex.Message);
                var response = new ResponseContract<RequestErrorPayload>("ERROR", payload);
                return BadRequest(response);
            }
        }

        [HttpPost("move")]
        public async Task<IActionResult> Move([FromBody] EvalMoveRequestContract request, CancellationToken cancellationToken)
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

                var options = new FeatureEvalOptions
                {
                    EvalMoveRequest = request,
                    TurnNumber = request.TurnNumber,
                    IsWhite = request.IsWhite,
                    ContactWeights = contactWeights,
                };

                var bestMove = await evalService.EvalMoveSequencesAsync(options, cancellationToken);
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
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
