
namespace GammonX.Engine.Models
{
    /// <summary>
    /// Represents an extension for the doubling cube in a backgammon variant.
    /// </summary>
    public interface IDoublingCubeModel
    {
        // TODO: implement increase doubling cube value
        // TODO: implement change doubling cube owner

        /// <summary>
        /// Gets the current value of the doubling cube.
        /// </summary>
        int DoublingCubeValue { get; }

        /// <summary>
        /// Returns true if black is the owner of the doubling cube.
        /// Returns false if white is the owner of the doubling cube.
        /// </summary>
        bool DoublingCubeOwner { get; }
    }
}
