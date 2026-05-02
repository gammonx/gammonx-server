using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// A race means both sides have fully passed each other.
    /// the lowest-indexed white checker sits at a higher index than the highest-indexed black checker. 
    /// </summary>
    public sealed class RaceFeature : IFeature<bool>
    {
        // <inheritdoc />
        public bool Eval(IBoardModel board, bool isWhite)
        {
            var maxBlackIndex = -1;
            var minWhiteIndex = board.Fields.Length;

            var fieldTuples = board.Fields.Index();

            var whiteIndices = fieldTuples.Where(t => t.Item < 0).Select(t => t.Index);
            var blackIndices = fieldTuples.Where(t => t.Item > 0).Select(t => t.Index);

            minWhiteIndex = whiteIndices.Any() ? whiteIndices.Min() : int.MaxValue;
            maxBlackIndex = blackIndices.Any() ? blackIndices.Max() : int.MinValue;

            // in Plakoto, a pinned checker is hidden from fields array: the pinners checker occupies
            // the field while the pinned checker is recorded only in pinned fields. A pinned
            // checker is still on the board and must be included in the contact check — omitting
            // it causes a false race when e.g. a white checker is pinned deep in blacks home.
            if (board is IPinModel pinModel)
            {
                var pinTuples = pinModel.PinnedFields.Index();

                var whitePinIndices = pinTuples.Where(t => t.Item < 0).Select(t => t.Index);
                var blackPinIndices = pinTuples.Where(t => t.Item > 0).Select(t => t.Index);

                var minPinWhiteIndex = whitePinIndices.Any() ? whitePinIndices.Min() : int.MaxValue;
                var maxPinBlackIndex = blackPinIndices.Any() ? blackPinIndices.Max() : int.MinValue;

                minWhiteIndex = Math.Min(minWhiteIndex, minPinWhiteIndex);
                maxBlackIndex = Math.Max(maxBlackIndex, maxPinBlackIndex);
            }

            // race: all black checkers are at lower indices than all white checkers
            // meaning both sides have passed each other with no contact remaining
            return minWhiteIndex >= maxBlackIndex;
        }
    }
}
