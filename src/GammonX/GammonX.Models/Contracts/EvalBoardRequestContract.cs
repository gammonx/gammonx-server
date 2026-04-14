using GammonX.Models.Contracts;

using GammonX.Models.Enums;

using System.Runtime.Serialization;

namespace GammonX.Models.Contracts
{
    [DataContract]
    public class EvalBoardRequestContract
    {
        [DataMember(Name = "modus", IsRequired = true)]
        public GameModus Modus { get; set; }

        [DataMember(Name = "board", IsRequired = true)]
        public BoardModelContract Board { get; set; } = new BoardModelContract();
    }
}
