
namespace GammonX.Engine.Models
{
    /// <summary>
    /// Represents an extension for the doubling cube in a backgammon variant.
    /// </summary>
    public interface IDoublingCubeModel
    {
        /// <summary>
        /// Gets the current value of the doubling cube.
        /// </summary>
        int DoublingCubeValue { get; }

        /// <summary>
        /// Returns true if black is the owner of the doubling cube.
        /// Returns false if white is the owner of the doubling cube.
        /// </summary>
        bool DoublingCubeOwner { get; set; }

		/// <summary>
		/// The doubling coube offer is accepted by the opponent.
		/// </summary>
		/// <remarks>
		/// The doubling cube value starts at 1 and is doubled at each accepted offer up to a maximum of 64.
		/// </remarks>
		void AcceptDoublingCubeOffer();

        /// <summary>
        /// Checks if the given board player can offer a doubling cube.
        /// </summary>
        /// <returns>Boolean indicating if doubling cube can be offered.</returns>
        bool CanOfferDoublingCube();
    }
}
