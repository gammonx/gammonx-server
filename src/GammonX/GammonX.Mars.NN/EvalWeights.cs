using GammonX.Mars.NN.Models;

namespace GammonX.Mars.NN
{
    public static class EvalWeights
    {
        public static readonly RaceWeightModel RaceWeights = new RaceWeightModel()
        {
            PipDifferenceWeight = 0.45,
            PipToBearOffWeight = 0.30,
            PipToBearOffOppWeight = 0.25,
        };

        public static readonly ContactWeightModel PlakotoContactWeights = new ContactWeightModel()
        {
            // priority 1: existing pin strength
            OppMotherPinnedWeight = 0.03,
            PlayerMotherPinnedWeight = 0.04,
            MotherDistancePlayerWeight = 0.02,
            MotherDistanceOppWeight = 0.02,
            NumChFrontLastPinWeight = 0.04,
            NumChFrontLastPinOppWeight = 0.04,
            PinCountOppWeight = 0.12,
            PinCountPlayerWeight = 0.12,
            BlotCountWeight = 0.05,
            BlotCountOppWeight = 0.04,
            BlotInStartRangeCountWeight = 0.02,
            BlotInStartRangeCountOppWeight = 0.02,
            AnchorCountWeight = 0.03,
            AnchorCountOppWeight = 0.03,
            // priority 2: future pin opportunities
            //HitOpponentProbability1Weight = 0.06,
            //HitOpponentProbability2Weight = 0.04,
            // priority 3: avoid getting pinned
            //HitProbability1Weight = 0.07,
            //HitProbability2Weight = 0.04,
            AverageStackHeightPlayerWeight = 0.10,
            AverageStackHeightOppWeight = 0.11,
            // priority 4: mobility / escape
            //EscapeProbability1Weight = 0.04,
            //EscapeProbability2Weight = 0.02,
            //EscapeProbability1OppWeight = 0.04,
            //EscapeProbability2OppWeight = 0.02,
            AverageDistanceToBearOffPlayerWeight = 0.06,
            AverageDistanceToBearOffOppWeight = 0.06,
            // priority 5: race
            PipDifferenceWeight = 0.03,
            PipToBearOffWeight = 0.01,
            PipToBearOffOppWeight = 0.01,
        };

        public static readonly ContactWeightModel PlakotoCheapContactWeights = new ContactWeightModel()
        {
            // priority 1: existing pin strength
            PinCountOppWeight = 0.09,
            PinCountPlayerWeight = 0.11,
            BlotCountWeight = 0.09,
            BlotCountOppWeight = 0.09,
            BlotInStartRangeCountWeight = 0.06,
            BlotInStartRangeCountOppWeight = 0.06,
            AnchorCountWeight = 0.08,
            AnchorCountOppWeight = 0.07,
            AverageStackHeightPlayerWeight = 0.02,
            AverageStackHeightOppWeight = 0.02,
            AverageDistanceToBearOffPlayerWeight = 0.02,
            AverageDistanceToBearOffOppWeight = 0.02,
            // priority 2: race
            PipDifferenceWeight = 0.12,
            PipToBearOffWeight = 0.10,
            PipToBearOffOppWeight = 0.05,
        };

        public static readonly ContactWeightModel FevgaContactWeights = new ContactWeightModel()
        {
            // priority 1: race
            PipDifferenceWeight = 0.16,
            PipToBearOffWeight = 0.10,
            PipToBearOffOppWeight = 0.10,
            // priority 2: prime and formation building
            MaxPrimeLengthPlayerWeight = 0.04,
            MaxPrimeLengthOppWeight = 0.04,
            HomebarCountPlayerWeight = 0.05,
            HomebarCountOppWeight = 0.04,
            BlotCountWeight = 0.04,
            BlotCountOppWeight = 0.04,
            //PrimeProbabilityPlayerWeight = 0.09,
            //PrimeProbabilityOppWeight = 0.10,
            AnchorCountInFrontPlayerWeight = 0.04,
            AnchorCountInFrontOppWeight = 0.04,
            AverageStackHeightPlayerWeight = 0.04,
            AverageStackHeightOppWeight = 0.04,
            AverageDistanceToBearOffPlayerWeight = 0.04,
            AverageDistanceToBearOffOppWeight = 0.04,
            AverageGapSizePlayerWeight = 0.04,
            AverageGapSizeOppWeight = 0.04,
            CheckersInPrimeZonePlayerWeight = 0.04,
            CheckersInPrimeZoneOppWeight = 0.03,
        };

        public static readonly ContactWeightModel FevgaCheapContactWeights = new ContactWeightModel()
        {
            // priority 1: race
            PipDifferenceWeight = 0.20,
            PipToBearOffWeight = 0.15,
            PipToBearOffOppWeight = 0.15,
            // priority 2: prime and formation building
            MaxPrimeLengthPlayerWeight = 0.07,
            MaxPrimeLengthOppWeight = 0.07,
            HomebarCountPlayerWeight = 0.04,
            HomebarCountOppWeight = 0.04,
            BlotCountWeight = 0.04,
            BlotCountOppWeight = 0.04,
            AnchorCountInFrontPlayerWeight = 0.02,
            AnchorCountInFrontOppWeight = 0.02,
            AverageStackHeightPlayerWeight = 0.02,
            AverageStackHeightOppWeight = 0.02,
            AverageDistanceToBearOffPlayerWeight = 0.02,
            AverageDistanceToBearOffOppWeight = 0.02,
            AverageGapSizePlayerWeight = 0.02,
            AverageGapSizeOppWeight = 0.02,
            CheckersInPrimeZonePlayerWeight = 0.02,
            CheckersInPrimeZoneOppWeight = 0.02,
        };
    }
}
