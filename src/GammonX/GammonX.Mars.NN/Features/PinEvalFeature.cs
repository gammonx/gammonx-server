using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Evaluates the current pin state of the board for both player and opponent.
    /// Reads <see cref="IPinModel.PinnedFields"/> directly — O(24), no probability computation needed.
    /// </summary>
    public sealed class PinEvalFeature : IFeature<PinEvalResult>
    {
        // <inheritdoc />
        public PinEvalResult Eval(IBoardModel board, bool isWhite)
        {
            if (board is not IPinModel pinModel)
                return new PinEvalResult(0, 0, 0, 0);

            var pinnedOppCount = 0;
            var pinnedPlayerCount = 0;

            for (var i = 0; i < pinModel.PinnedFields.Length; i++)
            {
                var field = pinModel.PinnedFields[i];
                pinnedOppCount += FeatureBoardGeometry.OwnCount(field, !isWhite);
                pinnedPlayerCount += FeatureBoardGeometry.OwnCount(field, isWhite);
            }

            int oppMotherIdx;
            int playerMotherIdx;
            if (isWhite)
            {
                oppMotherIdx = board.StartRangeBlack.Start.Value;
                playerMotherIdx = board.StartRangeWhite.Start.Value;
            }
            else
            {
                oppMotherIdx = board.StartRangeWhite.Start.Value;
                playerMotherIdx = board.StartRangeBlack.Start.Value;
            }

            var oppMotherPinned = FeatureBoardGeometry.OwnCount(pinModel.PinnedFields[oppMotherIdx], !isWhite) > 0 ? 1 : 0;

            var playerMotherPinned = FeatureBoardGeometry.OwnCount(pinModel.PinnedFields[playerMotherIdx], isWhite) > 0 ? 1 : 0;

            return new PinEvalResult(pinnedOppCount, pinnedPlayerCount, oppMotherPinned, playerMotherPinned);
        }
    }

    public readonly record struct PinEvalResult(
        int PinnedOppCount,
        int PinnedPlayerCount,
        int OppMotherPinned,
        int PlayerMotherPinned
    );
}
