using GammonX.Models.Enums;

using System.Runtime.Serialization;

namespace GammonX.Models.Contracts
{
    [DataContract]
    public class EvalCubeRequestContract
    {
        [DataMember(Name = "modus", IsRequired = true)]
        public GameModus Modus { get; set; }

        [DataMember(Name = "isWhite", IsRequired = true)]
        public bool IsWhite { get; set; }

        [DataMember(Name = "board", IsRequired = true)]
        public BoardModelContract Board { get; set; } = new BoardModelContract();
    }
}
