using GammonX.Mars.Server.Contracts;
using GammonX.Mars.Server.Models;
using GammonX.Mars.Server.Services;

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

        // TODO: get weighting from config

        private readonly RaceWeightModel _raceWeights = new RaceWeightModel()
        {
            PipToBearOffOppWeight = 0.45,
            PipToBearOffWeight = 0.35,
            PipDifferenceWeight = 0.20
        };

        private readonly ContactWeightModel _contactWeights = new ContactWeightModel()
        {
            // priority 1: existing pin strength (0.36)
            OppMotherPinnedWeight = 0.05,        // binary but game-defining — highest single weight
            PlayerMotherPinnedWeight = 0.05,     // symmetric: bot mother pinned = near-loss
            NumChFrontLastPinWeight = 0.06,      // continuous, captures gammon potential
            NumChFrontLastPinOppWeight = 0.06,   // fewer opp checkers past your pin = pin more effective
            PinCountOppWeight = 0.07,            // raw count, less specific than positional features above
            PinCountPlayerWeight = 0.07,
            // priority 2: future pin opportunities (0.22)
            HitOpponentProbability1Weight = 0.13, // most common path to creating a new pin
            HitOpponentProbability2Weight = 0.09, // double pin is rarer but stronger
            // priority 3: avoid getting pinned (0.18)
            HitProbability1Weight = 0.11,
            HitProbability2Weight = 0.07,
            // priority 4: mobility / escape (0.16)
            EscapeProbability1Weight = 0.05,
            EscapeProbability2Weight = 0.03,
            EscapeProbability1OppWeight = 0.05,
            EscapeProbability2OppWeight = 0.03,
            // priority 5: race (minor in contact, 0.08)
            PipDifferenceWeight = 0.04,
            PipToBearOffWeight = 0.02,
            PipToBearOffOppWeight = 0.02,
        };

        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }

        [HttpPost("board")]
        public IActionResult Board([FromBody] MoveRequestContract request)
        {
            try
            {
                var evalService = _serviceProvider.GetRequiredKeyedService<IFeatureEvalService>(request.Modus);

                _raceWeights.Validate();
                _contactWeights.Validate();

                var boardScore = evalService.EvalBoardState(request, _contactWeights, _raceWeights);
                var payload = new BoardEvalPayload() { EvalScore = boardScore };
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
        public IActionResult Move([FromBody] MoveRequestContract request)
        {
            try
            {
                var evalService = _serviceProvider.GetRequiredKeyedService<IFeatureEvalService>(request.Modus);

                _raceWeights.Validate();
                _contactWeights.Validate();

                var bestMove = evalService.EvalMoveSequence(request, _contactWeights, _raceWeights);
                var payload = new MoveEvalPayload() { MoveSequence = bestMove };
                var response = new ResponseContract<MoveEvalPayload>("OK", payload);
                return Ok(response);
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
