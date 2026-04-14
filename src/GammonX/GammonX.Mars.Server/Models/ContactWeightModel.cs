using System.Runtime.Serialization;

namespace GammonX.Mars.Server.Models
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

        public void Validate()
        {
            if (Math.Abs(
                HitProbability1Weight +
                HitProbability2Weight +
                HitOpponentProbability1Weight +
                HitOpponentProbability2Weight +
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
                PlayerMotherPinnedWeight
                - 1.0)
                > 1e-9)
            {
                throw new InvalidOperationException("weights do not sum up to 1.0");
            }
        }
    }
}
