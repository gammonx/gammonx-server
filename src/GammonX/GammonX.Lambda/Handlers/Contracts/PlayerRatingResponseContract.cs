using System.Runtime.Serialization;

namespace GammonX.Lambda.Handlers.Contracts
{
    /// <summary>
    /// Provides a minimum contract for player rating response.
    /// </summary>
    [DataContract]
    public sealed class PlayerRatingResponseContract : BaseResponseContract
    {
        [DataMember(Name = "Rating")]
        public double Rating { get; set; } = 0;
    }
}
