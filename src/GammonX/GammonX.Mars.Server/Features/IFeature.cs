using GammonX.Engine.Models;
using GammonX.Engine.Services;

namespace GammonX.Mars.Server.Features
{
    /// <summary>
    /// Marker interface for features that can be evaluated on a given board state.
    /// </summary>
    /// <typeparam name="T">Non normalized evaluation result type.</typeparam>
    public interface IFeature<T>
    {
        /// <summary>
        /// Provides the capability to evaluate a specific feature for a given board state.
        /// </summary>
        /// <param name="board">Board to evaluate on.</param>
        /// <param name="isWhite">Indicating if white or black checkers.</param>
        /// <returns>A non normalized evaluation result.</returns>
        public T Eval(IBoardModel board, bool isWhite);
    }
}
