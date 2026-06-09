using GammonX.Models.Enums;

using System.Runtime.Serialization;

namespace GammonX.Models.Contracts
{
    [DataContract]
    public class EvalMoveRequestContract
    {
        [DataMember(Name = "modus", IsRequired = true)]
        public GameModus Modus { get; set; }

        [DataMember(Name = "isWhite", IsRequired = true)]
        public bool IsWhite { get; set; }

        [DataMember(Name = "rolls", IsRequired = true )]
        public int[] Rolls { get; set; } = Array.Empty<int>();

        [DataMember(Name = "board", IsRequired = true)]
        public BoardModelContract Board { get; set; } = new BoardModelContract();

        [DataMember(Name = "botLevel", IsRequired = true)]
        public BotLevel BotLevel { get; set; } = BotLevel.Unknown;
    }
}
