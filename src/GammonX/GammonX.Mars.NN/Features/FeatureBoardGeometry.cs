using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features;

/// <summary>
/// Provides owner-relative coordinates and checker counts for board features.
/// </summary>
/// <remarks>
/// Movement positions run from 0 at the owner's first point to 23 at its last point.
/// Physical board indices retain the engine convention: white checkers are negative,
/// black checkers are positive. Fevga's black movement path wraps from index 23 to 0.
/// Coordinate methods accept board points only; bar/reserve is handled separately.
/// </remarks>
internal static class FeatureBoardGeometry
{
    /// <summary>
    /// Converts a physical board index to its position along the owners movement path.
    /// </summary>
    /// <param name="board">Board supplying the game-specific movement rules.</param>
    /// <param name="isWhite">The owner whose movement direction is used.</param>
    /// <param name="index">Physical board index in [0, 23].</param>
    /// <returns>A position in [0, 23], increasing toward bearing off.</returns>
    public static int MovementPosition(IBoardModel board, bool isWhite, int index)
    {
        var start = isWhite ? board.StartRangeWhite.Start.Value : board.StartRangeBlack.Start.Value;
        return board.RecoverRollOperator(isWhite, start, index);
    }

    /// <summary>
    /// Converts an owner-relative movement position to a physical board index.
    /// This is the inverse of <see cref="MovementPosition"/> for board points.
    /// </summary>
    /// <param name="board">Board supplying the game-specific movement rules.</param>
    /// <param name="isWhite">The owner whose movement direction is used.</param>
    /// <param name="position">Movement position in [0, 23], starting at the owner's first point.</param>
    /// <returns>The corresponding physical board index in [0, 23].</returns>
    public static int BoardIndex(IBoardModel board, bool isWhite, int position)
    {
        var start = isWhite ? board.StartRangeWhite.Start.Value : board.StartRangeBlack.Start.Value;
        return board.MoveOperator(isWhite, start, position);
    }

    /// <summary>
    /// Gets the geometric pip distance from a board point to bearing off,
    /// including the final pip that removes the checker from the board.
    /// </summary>
    /// <param name="board">Board supplying the game-specific movement rules.</param>
    /// <param name="isWhite">The checker owner.</param>
    /// <param name="index">Physical board index in [0, 23].</param>
    /// <returns>24 at the owner's first point, decreasing to 1 at its last point.</returns>
    public static int BearOffDistance(IBoardModel board, bool isWhite, int index) =>
        board.Fields.Length - MovementPosition(board, isWhite, index);

    /// <summary>
    /// Extracts the nonnegative count belonging to one owner from a signed field value.
    /// Works with either visible fields or pinned fields.
    /// </summary>
    /// <param name="field">A signed checker count: negative for white, positive for black.</param>
    /// <param name="isWhite">The checker owner to count.</param>
    /// <returns>The count's magnitude for the requested owner, or zero for an empty/opposing field.</returns>
    public static int OwnCount(int field, bool isWhite) => isWhite ? Math.Max(-field, 0) : Math.Max(field, 0);

    /// <summary>
    /// Counts the owner's visible and hidden pinned checkers at one board point.
    /// Bar/reserve and borne-off checkers are excluded.
    /// </summary>
    /// <param name="board">Board whose visible and optional pinned fields are counted.</param>
    /// <param name="isWhite">The checker owner to count.</param>
    /// <param name="index">Physical board index in [0, 23].</param>
    /// <returns>The owner's remaining checker count at the point, independent of the pinner's color.</returns>
    public static int RemainingCountAt(IBoardModel board, bool isWhite, int index) =>
        OwnCount(board.Fields[index], isWhite)
        + (board is IPinModel pins ? OwnCount(pins.PinnedFields[index], isWhite) : 0);

    /// <summary>
    /// Counts the owner's checkers on the hit bar or in Fevga's reserve.
    /// </summary>
    /// <param name="board">Board that may provide bar/reserve state.</param>
    /// <param name="isWhite">The checker owner to count.</param>
    /// <returns>The bar/reserve count, or zero for a board without <see cref="IHomeBarModel"/>.</returns>
    public static int BarCount(IBoardModel board, bool isWhite) => board is IHomeBarModel bar
        ? isWhite ? bar.HomeBarCountWhite : bar.HomeBarCountBlack
        : 0;

    /// <summary>
    /// Determines whether the owner has a visible singleton that is not holding a pin.
    /// Counts singleton points in Fevga too, where one checker also blocks a point.
    /// </summary>
    /// <param name="board">Board supplying visible fields and optional pin state.</param>
    /// <param name="isWhite">The owner of the singleton.</param>
    /// <param name="index">Physical board index in [0, 23].</param>
    /// <returns>Whether the point contains exactly one own checker and no pinned checker.</returns>
    public static bool IsBlot(IBoardModel board, bool isWhite, int index) =>
        OwnCount(board.Fields[index], isWhite) == 1
        && (board is not IPinModel pins || pins.PinnedFields[index] == 0);

    /// <summary>
    /// Determines whether the owner blocks a point under the board's rules.
    /// A Plakoto singleton holding an opponent pin also makes the point.
    /// </summary>
    /// <param name="board">Board supplying the blocking threshold and optional pin state.</param>
    /// <param name="isWhite">The owner of the blocking checkers.</param>
    /// <param name="index">Physical board index in [0, 23].</param>
    /// <returns>Whether the point is made by the requested owner.</returns>
    public static bool IsMadePoint(IBoardModel board, bool isWhite, int index)
    {
        var count = OwnCount(board.Fields[index], isWhite);
        return count >= board.BlockAmount
            || (count == board.BlockAmount - 1 && board is IPinModel pins
                && OwnCount(pins.PinnedFields[index], !isWhite) > 0);
    }

    /// <summary>
    /// Gets the greatest bear-off distance among the owner's remaining checkers,
    /// including visible checkers, hidden pins, and bar/reserve.
    /// </summary>
    /// <param name="board">Board whose remaining checker inventory is examined.</param>
    /// <param name="isWhite">The checker owner.</param>
    /// <returns>
    /// Zero when no checkers remain, 1-24 for the rearmost board checker,
    /// or 25 when at least one checker is on bar/reserve.
    /// </returns>
    public static int RearDistance(IBoardModel board, bool isWhite)
    {
        // Zero means no remaining checkers. Bar/reserve is behind every board point.
        if (BarCount(board, isWhite) > 0)
        {
            return board.Fields.Length + 1;
        }

        var distance = 0;
        for (var index = 0; index < board.Fields.Length; index++)
        {
            if (RemainingCountAt(board, isWhite, index) > 0)
            {
                distance = Math.Max(distance, BearOffDistance(board, isWhite, index));
            }
        }

        return distance;
    }
}
