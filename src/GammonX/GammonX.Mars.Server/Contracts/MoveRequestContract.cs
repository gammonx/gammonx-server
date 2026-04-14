using GammonX.Engine.Contracts;

using GammonX.Models.Enums;

using System.Runtime.Serialization;

namespace GammonX.Mars.Server.Contracts
{
    [DataContract]
    public class MoveRequestContract
    {
        [DataMember(Name = "modus", IsRequired = true)]
        public GameModus Modus { get; set; }

        [DataMember(Name = "rolls", IsRequired = true )]
        public int[] Rolls { get; set; } = Array.Empty<int>();

        [DataMember(Name = "board", IsRequired = true)]
        public BoardModelContract Board { get; set; } = new BoardModelContract();
    }
}
