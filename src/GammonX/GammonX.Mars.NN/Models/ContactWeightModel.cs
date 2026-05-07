using System.Runtime.Serialization;

namespace GammonX.Mars.NN.Models
{
    /// <summary>
    /// Weight model config for <see cref="EvalResultModel"/> evaluation model."/>
    /// </summary>
    [DataContract]
    public class ContactWeightModel
    {
        [DataMember]
        public double HitProbability1Weight { get; set; }

        [DataMember]
        public double HitProbability2Weight { get; set; }

        [DataMember]
        public double HitOpponentProbability1Weight { get; set; }

        [DataMember]
        public double HitOpponentProbability2Weight { get; set; }

        [DataMember]
        public double BlotCountWeight { get; set; }

        [DataMember]
        public double BlotCountOppWeight { get; set; }

        [DataMember]
        public double BlotInStartRangeCountWeight { get; set; }

        [DataMember]
        public double AnchorCountWeight { get; set; }

        [DataMember]
        public double PipDifferenceWeight { get; set; }

        [DataMember]
        public double PipToBearOffWeight { get; set; }

        [DataMember]
        public double PipToBearOffOppWeight { get; set; }

        [DataMember]
        public double NumChFrontLastPinWeight { get; set; }

        [DataMember]
        public double NumChFrontLastPinOppWeight { get; set; }

        [DataMember]
        public double EscapeProbability1Weight { get; set; }

        [DataMember]
        public double EscapeProbability2Weight { get; set; }

        [DataMember]
        public double EscapeProbability1OppWeight { get; set; }

        [DataMember]
        public double EscapeProbability2OppWeight { get; set; }

        [DataMember]
        public double PinCountOppWeight { get; set; }

        [DataMember]
        public double PinCountPlayerWeight { get; set; }

        [DataMember]
        public double OppMotherPinnedWeight { get; set; }

        [DataMember]
        public double PlayerMotherPinnedWeight { get; set; }

        [DataMember]
        public double MaxPrimeLengthPlayerWeight { get; set; }

        [DataMember]
        public double MaxPrimeLengthOppWeight { get; set; }

        [DataMember]
        public double HomebarCountPlayerWeight { get; set; }

        [DataMember]
        public double PrimeProbabilityPlayerWeight { get; set; }

        [DataMember]
        public double PrimeProbabilityOppWeight { get; set; }

        [DataMember]
        public double AnchorCountInFrontPlayerWeight { get; init; } = 0.0;

        [DataMember]
        public double AnchorCountInFrontOppWeight { get; init; } = 0.0;

        [DataMember]
        public double AverageStackHeightPlayerWeight { get; init; } = 0.0;

        [DataMember]
        public double AverageStackHeightOppWeight { get; init; } = 0.0;

        [DataMember]
        public double AverageDistanceToBearOffPlayerWeight { get; init; } = 0.0;

        [DataMember]
        public double AverageDistanceToBearOffOppWeight { get; init; } = 0.0;

        [DataMember]
        public double AverageGapSizePlayerWeight { get; init; } = 0.0;

        [DataMember]
        public double AverageGapSizeOppWeight { get; init; } = 0.0;

        [DataMember]
        public double CheckersInPrimeZonePlayerWeight { get; init; } = 0.0;

        [DataMember]
        public double CheckersInPrimeZoneOppWeight { get; init; } = 0.0;

        public void Validate()
        {
            if (Math.Abs(
                HitProbability1Weight +
                HitProbability2Weight +
                HitOpponentProbability1Weight +
                HitOpponentProbability2Weight +
                BlotCountWeight +
                BlotCountOppWeight +
                BlotInStartRangeCountWeight +
                AnchorCountWeight +
                PipDifferenceWeight +
                PipToBearOffWeight +
                PipToBearOffOppWeight +
                NumChFrontLastPinWeight +
                NumChFrontLastPinOppWeight +
                EscapeProbability1Weight +
                EscapeProbability2Weight +
                EscapeProbability1OppWeight +
                EscapeProbability2OppWeight +
                PinCountOppWeight +
                PinCountPlayerWeight +
                OppMotherPinnedWeight +
                PlayerMotherPinnedWeight +
                MaxPrimeLengthPlayerWeight +
                MaxPrimeLengthOppWeight +
                HomebarCountPlayerWeight +
                PrimeProbabilityPlayerWeight +
                PrimeProbabilityOppWeight +
                AnchorCountInFrontPlayerWeight +
                AnchorCountInFrontOppWeight +
                AverageStackHeightPlayerWeight +
                AverageStackHeightOppWeight +
                AverageDistanceToBearOffPlayerWeight +
                AverageDistanceToBearOffOppWeight +
                AverageGapSizePlayerWeight +
                AverageGapSizeOppWeight +
                CheckersInPrimeZonePlayerWeight +
                CheckersInPrimeZoneOppWeight
                - 1.0)
                > 1e-9)
            {
                throw new InvalidOperationException("weights do not sum up to 1.0");
            }
        }
    }
}
