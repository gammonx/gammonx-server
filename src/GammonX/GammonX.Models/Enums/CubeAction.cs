namespace GammonX.Models.Enums
{
    public enum CubeAction
    {
        /// <summary>
        /// The player would lose match equity if a double is offered to the opponent.
        /// </summary>
        NoDouble = 0,
        /// <summary>
        /// The player would win match equity if a double is offered to the opponent.
        /// </summary>
        Double = 1,
        /// <summary>
        /// The players board position is so good that offering a double would actually lower the match equity,
        /// because the opponent is at risk to give away more points (e.g. in a gammon or backgammon loss).
        /// </summary>
        TooGood = 2,
    }
}
