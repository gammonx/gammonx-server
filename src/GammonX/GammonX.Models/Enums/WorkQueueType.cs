using System.ComponentModel.DataAnnotations;

namespace GammonX.Models.Enums
{
    public enum WorkQueueType
    {
        [Display(Name = "GAME_COMPLETED")]
        GameCompleted = 0,
        [Display(Name = "MATCH_COMPLETED")]
        MatchCompleted = 1,
        [Display(Name = "PLAYER_CREATED")]
        PlayerCreated = 2,
        [Display(Name = "RATING_UPDATED")]
        RatingUpdated = 3,
        [Display(Name = "STATS_UPDATED")]
        StatsUpdated = 4
    }
}
