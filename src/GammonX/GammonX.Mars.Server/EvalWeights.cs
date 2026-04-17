using GammonX.Mars.Server.Models;

namespace GammonX.Mars.Server
{
    public static class EvalWeights
    {
        // TODO: get weighting from config

        public static readonly RaceWeightModel RaceWeights = new RaceWeightModel()
        {
            PipDifferenceWeight = 0.45,
            PipToBearOffWeight = 0.30,
            PipToBearOffOppWeight = 0.25,
        };

        public static readonly ContactWeightModel PlakotoContactWeights = new ContactWeightModel()
        {
            // priority 1: existing pin strength
            OppMotherPinnedWeight = 0.05,
            PlayerMotherPinnedWeight = 0.06,
            NumChFrontLastPinWeight = 0.04,
            NumChFrontLastPinOppWeight = 0.04,
            PinCountOppWeight = 0.14,
            PinCountPlayerWeight = 0.14,
            BlotCountWeight = 0.05,
            BlotInStartRangeCountWeight = 0.04, 
            AnchorCountWeight = 0.6,
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
            PinCountOppWeight = 0.11,
            PinCountPlayerWeight = 0.13,
            BlotCountWeight = 0.20,
            BlotInStartRangeCountWeight = 0.14,
            AnchorCountWeight = 0.15,
            // priority 2: race
            PipDifferenceWeight = 0.12,
            PipToBearOffWeight = 0.10,
            PipToBearOffOppWeight = 0.05,
        };

        public static readonly ContactWeightModel FevgaContactWeights = new ContactWeightModel()
        {
            // priority 1: race
            PipDifferenceWeight = 0.25,
            PipToBearOffWeight = 0.15,
            PipToBearOffOppWeight = 0.15,
            // priority 2: prime and formation building
            MaxPrimeLengthPlayerWeight = 0.08,
            MaxPrimeLengthOppWeight = 0.08,
            HomebarCountPlayerWeight = 0.05,
            BlotCountWeight = 0.04,
            PrimeProbabilityPlayerWeight = 0.10,
            PrimeProbabilityOppWeight = 0.10,
        };

        public static readonly ContactWeightModel FevgaCheapContactWeights = new ContactWeightModel()
        {
            // priority 1: race
            PipDifferenceWeight = 0.30,
            PipToBearOffWeight = 0.20,
            PipToBearOffOppWeight = 0.20,
            // priority 2: prime and formation building
            MaxPrimeLengthPlayerWeight = 0.10,
            MaxPrimeLengthOppWeight = 0.10,
            HomebarCountPlayerWeight = 0.05,
            BlotCountWeight = 0.05,
        };
    }
}
