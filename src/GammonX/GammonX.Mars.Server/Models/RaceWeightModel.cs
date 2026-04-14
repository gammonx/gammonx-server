using System.Runtime.Serialization;

namespace GammonX.Mars.Server.Models
{
    /// <summary>
    /// Weight model config for <see cref="EvalResultModel"/> evaluation model."/>
    /// </summary>
    [DataContract]
    public class RaceWeightModel
    {
        [DataMember]
        public double PipDifferenceWeight { get; set; }

        [DataMember]
        public double PipToBearOffWeight { get; set; }

        [DataMember]
        public double PipToBearOffOppWeight { get; set; }

        public void Validate()
        {
            if (Math.Abs(PipDifferenceWeight + PipToBearOffWeight + PipToBearOffOppWeight -1.0) > 1e-9)
            {
                throw new InvalidOperationException("weights do not sum up to 1.0");
            }
        }
    }
}
