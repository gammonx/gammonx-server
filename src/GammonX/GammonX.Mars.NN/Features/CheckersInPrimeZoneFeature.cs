using GammonX.Engine.Models;

using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Counts the amount of checkers for the given player in the prime zone (e.g. mid board)
    /// </summary>
    /// <remarks>
    /// Only applicable for Fevga at the moment based on the prime zone definition.
    /// </remarks>
    public class CheckersInPrimeZoneFeature : IFeature<int>
    {
        // <inheritdoc />
        public int Eval(IBoardModel board, bool isWhite)
        {
            if (board.Modus == GameModus.Fevga)
            {
                // fevga has a different behavior and start board layout
                if (isWhite)
                {
                    var whitePositions = board.Fields.Index().Where(i => i.Item < 0).ToList();
                    if (whitePositions.Count != 0)
                    {
                        var primeZone = board.StartRangeBlack.Start.Value..(board.StartRangeBlack.End.Value + 6);
                        var primeCheckers = whitePositions.Where(wp => wp.Index >= primeZone.Start.Value && wp.Index <= primeZone.End.Value);
                        return Math.Abs(primeCheckers.Sum(pc => pc.Item));
                    }
                    return 0;
                }
                else
                {
                    var blackPositions = board.Fields.Index().Where(i => i.Item > 0).ToList();
                    if (blackPositions.Count != 0)
                    {
                        var primeZone = board.StartRangeWhite.Start.Value..(board.StartRangeWhite.End.Value + 6);
                        var primeCheckers = blackPositions.Where(bp => bp.Index >= primeZone.Start.Value && bp.Index <= primeZone.End.Value);
                        return primeCheckers.Sum(pc => pc.Item);
                    }
                    return 0;
                }
            }
            else
            {
                if (isWhite)
                {
                    var whitePositions = board.Fields.Index().Where(i => i.Item < 0).ToList();
                    if (whitePositions.Count != 0)
                    {
                        var primeZone = (board.StartRangeWhite.End.Value + 1)..(board.StartRangeBlack.End.Value - 1);
                        var primeCheckers = whitePositions.Where(wp => wp.Index >= primeZone.Start.Value && wp.Index <= primeZone.End.Value);
                        return Math.Abs(primeCheckers.Sum(pc => pc.Item));
                    }
                    return 0;
                }
                else
                {
                    var blackPositions = board.Fields.Index().Where(i => i.Item > 0).ToList();
                    if (blackPositions.Count != 0)
                    {
                        var primeZone = (board.StartRangeWhite.End.Value + 1)..(board.StartRangeBlack.End.Value - 1);
                        var primeCheckers = blackPositions.Where(bp => bp.Index >= primeZone.Start.Value && bp.Index <= primeZone.End.Value);
                        return primeCheckers.Sum(pc => pc.Item);
                    }
                    return 0;
                }
            }
        }
    }
}
