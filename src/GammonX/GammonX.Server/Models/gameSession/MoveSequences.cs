using GammonX.Engine.Models;

using GammonX.Models.Enums;

namespace GammonX.Server.Models
{
	public class MoveSequences : List<MoveSequenceModel>
	{
		public bool CanMove => this.Any(ms => ms.Moves.Count > 0);

		public MoveSequences()
		{
			// pass
		}

        /// <summary>
        /// Tries to use the given from/to move and evaluates which move sequences were used.
        /// Enforces strict contiguity and exact matching order.
        /// </summary>
        /// <param name="from">From move index.</param>
        /// <param name="to">To move index.</param>
        /// <param name="playedMoves">Played moves.</param>
        /// <returns>A boolean indicating if the given from to move was present.</returns>
        public bool TryUseMove(int from, int to, out List<MoveModel> playedMoves)
		{
            playedMoves = new();

            var sequences = this.ToList();

            // exact single move always wins (anywhere)
            foreach (var seq in sequences)
            {
                var exact = seq.Moves.FirstOrDefault(m => m.From == from && m.To == to);

                if (exact != null)
                {
                    playedMoves.Add(exact);
                    return true;
                }
            }

            // secondly combined move = contiguous PREFIX (length 2–4)
            foreach (var seq in sequences)
            {
                var moves = seq.Moves;
                if (moves.Count < 2)
                    continue;

                // must start at from
                if (moves[0].From != from)
                    continue;

                int current = from;

                for (int i = 0; i < moves.Count; i++)
                {
                    var move = moves[i];

                    // strict contiguity
                    if (move.From != current)
                        break;

                    current = move.To;

                    // prefix matches exactly
                    if (current == to)
                    {
                        playedMoves.AddRange(moves.Take(i + 1));
                        return true;
                    }
                }
            }

            return false;
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
