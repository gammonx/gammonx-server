using GammonX.Engine.Models;

using GammonX.Mars.Server.Contracts;
using GammonX.Mars.Server.Models;

namespace GammonX.Mars.Server.Services
{
    // TODO implement

    // <inheritdoc />
    public sealed class FevgaFeatureEvalService : IFeatureEvalService
    {
        // <inheritdoc />
        public double EvalBoardState(EvalBoardRequestContract contract, ContactWeightModel contactWeights, RaceWeightModel raceWeights)
        {
            throw new NotImplementedException();
        }

        // <inheritdoc />
        public MoveSequenceModel EvalMoveSequence(EvalMoveRequestContract contract, ContactWeightModel contactWeights, RaceWeightModel raceWeights)
        {
            throw new NotImplementedException();
        }
    }
}
