using System.Runtime.Serialization;

using GammonX.Models.Enums;

namespace GammonX.Models.Contracts
{
    [DataContract]
    public sealed class PlayerGamesResponseContract : BaseResponseContract
    {
        [DataMember(Name = "Games")]
        public List<GameSummaryContract> Games { get; set; } = new();
    }

    [DataContract]
    public sealed class GameSummaryContract
    {
        [DataMember(Name = "Id")]
        public Guid Id { get; set; }

        [DataMember(Name = "MatchId")]
        public Guid MatchId { get; set; }

        [DataMember(Name = "Result")]
        public GameResult Result { get; set; }

        [DataMember(Name = "Modus")]
        public GameModus Modus { get; set; }

        [DataMember(Name = "Points")]
        public int Points { get; set; }

        [DataMember(Name = "Length")]
        public int Length { get; set; }

        [DataMember(Name = "StartedAt")]
        public DateTime StartedAt { get; set; }

        [DataMember(Name = "EndedAt")]
        public DateTime EndedAt { get; set; }

        [DataMember(Name = "Duration")]
        public TimeSpan Duration { get; set; }

        [DataMember(Name = "PipesLeft")]
        public int PipesLeft { get; set; }

        [DataMember(Name = "DiceDoubles")]
        public int DiceDoubles { get; set; }

        [DataMember(Name = "DoublingCubeValue")]
        public int? DoublingCubeValue { get; set; }
    }
}
