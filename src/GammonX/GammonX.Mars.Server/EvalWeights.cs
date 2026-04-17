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
            // priority 1: existing pin strength
            OppMotherPinnedWeight = 0.05,
            PlayerMotherPinnedWeight = 0.06,
            NumChFrontLastPinWeight = 0.04,
            NumChFrontLastPinOppWeight = 0.04,
            PinCountOppWeight = 0.08,
            PinCountPlayerWeight = 0.08,
            BlotCountWeight = 0.09,
            BlotInStartRangeCountWeight = 0.08, 
            AnchorCountWeight = 0.10,
            // priority 2: future pin opportunities
            HitOpponentProbability1Weight = 0.06,
            HitOpponentProbability2Weight = 0.04,
            // priority 3: avoid getting pinned
            HitProbability1Weight = 0.07,
            HitProbability2Weight = 0.04,
            // priority 4: mobility / escape
            EscapeProbability1Weight = 0.04,
            EscapeProbability2Weight = 0.02,
            EscapeProbability1OppWeight = 0.04,
            EscapeProbability2OppWeight = 0.02,
            // priority 5: race
            PipDifferenceWeight = 0.03,
            PipToBearOffWeight = 0.01,
            PipToBearOffOppWeight = 0.01,
        };

        public static readonly ContactWeightModel PlakotoCheapContactWeights = new ContactWeightModel()
        {
            // priority 1: existing pin strength
            OppMotherPinnedWeight = 0.00,
            PlayerMotherPinnedWeight = 0.00,
            NumChFrontLastPinWeight = 0.00,
            NumChFrontLastPinOppWeight = 0.00,
            PinCountOppWeight = 0.11,
            PinCountPlayerWeight = 0.13,
            BlotCountWeight = 0.20,
            BlotInStartRangeCountWeight = 0.14,
            AnchorCountWeight = 0.15,
            // priority 2: future pin opportunities
            HitOpponentProbability1Weight = 0.00,
            HitOpponentProbability2Weight = 0.00,
            // priority 3: avoid getting pinned
            HitProbability1Weight = 0.00,
            HitProbability2Weight = 0.00,
            // priority 4: mobility / escape
            EscapeProbability1Weight = 0.00,
            EscapeProbability2Weight = 0.00,
            EscapeProbability1OppWeight = 0.00,
            EscapeProbability2OppWeight = 0.00,
            // priority 5: race
            PipDifferenceWeight = 0.12,
            PipToBearOffWeight = 0.10,
            PipToBearOffOppWeight = 0.05,
        };
    }
}
