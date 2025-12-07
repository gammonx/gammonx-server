namespace GammonX.Server.Queue
{
    public enum WorkQueueType
    {
        GameCompleted = 0,
        MatchCompleted = 1,
        PlayerCreated = 2,
        RatingUpdated = 3,
        StatsUpdated = 4
    }
}
