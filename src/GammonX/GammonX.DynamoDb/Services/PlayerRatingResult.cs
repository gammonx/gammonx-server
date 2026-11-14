using GammonX.DynamoDb.Items;

namespace GammonX.DynamoDb.Services
{
    /// <summary>
    /// Contains an updated player rating and the pre-match period used to calculate it.
    /// </summary>
    /// <param name="Rating">Updated player rating, ready to be committed.</param>
    /// <param name="Period">Related period containing the pre-match rating inputs.</param>
    public readonly record struct PlayerRatingResult(PlayerRatingItem Rating, RatingPeriodItem Period);
}
