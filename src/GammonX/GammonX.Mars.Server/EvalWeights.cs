using GammonX.Mars.Server.Models;

namespace GammonX.Mars.Server
{
    public static class EvalWeights
    {
        // TODO: get weighting from config

        public static readonly RaceWeightModel PlakotoRaceWeights = new RaceWeightModel()
        {
            PipToBearOffOppWeight = 0.45,
            PipToBearOffWeight = 0.35,
            PipDifferenceWeight = 0.20
        };

        public static readonly ContactWeightModel PlakotoContactWeights = new ContactWeightModel()
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
    }
}
