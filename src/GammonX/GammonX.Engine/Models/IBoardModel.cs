namespace GammonX.Engine.Models
{
    /// <summary>
    /// Represents the basic model of a game board in a backgammon variant.
    /// </summary>
    public interface IBoardModel
    {
        /// <summary>
        /// Gets the game mode on which the board is based.
        /// </summary>
        GameModus Modus { get; }

        /// <summary>
        /// Gets an array representing the points on the board.
        /// </summary>
        /// <remarks>
        /// Negative numbers in the array represent the number of pieces for the white player, 
        /// while positive numbers represent the peices for the black player.
        /// </remarks>
        int[] Points { get; }

        /// <summary>
        /// Gets the index of the home point for the white player.
        /// </summary>
        int WhiteHome { get; }

        /// <summary>
        /// Gets the index of the home point for the black player.
        /// </summary>
        int BlackHome { get; }

        /// <summary>
        /// Gets the amount of pieces for the white player that are currently borne off.
        /// </summary>
        int BearOffWhite { get; }

        /// <summary>
        /// Gets the amount of pieces for the black player that are currently borne off.
        /// </summary>
        int BearOffBlack { get; }
    }
}
