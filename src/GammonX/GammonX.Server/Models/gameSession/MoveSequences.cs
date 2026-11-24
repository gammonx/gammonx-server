using GammonX.Engine.Models;

using GammonX.Models.Enums;

namespace GammonX.Server.Models
{
	public class MoveSequences : List<MoveSequenceModel>
	{
		public bool CanMove => this.Any(ms => ms.Moves.Count > 0);

        public MoveSequences(IEnumerable<MoveSequenceModel> sequences)
        {
            this.AddRange(sequences);
        }

		public MoveSequences()
		{
			// pass
		}

		/// <summary>
		/// Creates a contract representation of the move sequences.
		/// </summary>
		/// <param name="inverted">Indictating if the given from/to moves should be converted horizontally.</param>
		/// <param name="modus">Decides on how the inversion is done.</param>
		/// <returns>A move sequence model array.</returns>
		public MoveSequenceModel[] ToContract(bool inverted, GameModus modus)
		{
			if (inverted)
			{
				return this.Select(ms => ms.Invert(modus)).ToArray();
			}
			return ToArray();
		}
	}
}
