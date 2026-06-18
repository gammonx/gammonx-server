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
        /// <summary>
        /// The player got offered a double by the opponent and should accept it, because declining would lower the match equity.
        /// </summary>
        Take = 3,
        /// <summary>
        /// The player got offered a double by the opponent and should decline it, because accepting would lower the match equity.
        /// </summary>
        Pass = 4,
        /// <summary>
        /// The player offered his opponent a double and is waiting for the opponent to either <see cref="Take"/> or <see cref="Pass"/> the double.
        /// </summary>
        Offer = 5,
        /// <summary>
        /// Default value.
        /// </summary>
        Unknown = 99
    }
}
