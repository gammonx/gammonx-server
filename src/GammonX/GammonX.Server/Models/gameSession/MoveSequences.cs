using GammonX.Engine.Models;

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
		/// </summary>
		/// <param name="from">From move index.</param>
		/// <param name="to">To move index.</param>
		/// <param name="remainingRolls">The remaining rolls.</param>
		/// <param name="playedMoves">Played moves.</param>
		/// <returns>A list of moves which can be played.</returns>
		public bool TryUseMove(IBoardModel model, int from, int to, int[] remainingRolls, out List<MoveModel> playedMoves)
		{
			playedMoves = new List<MoveModel>();

			// the given from index act as a constraint for the possible move seuqences
			var potentialMoveSequences = this.ToList();
			// evaluate the move seuqences which contains a move sequence with the given to index
			foreach (MoveSequenceModel moveSeq in potentialMoveSequences)
			{
				var moves = moveSeq.DeepClone().Moves.ToList();
				// we iterate from the first move and check if there was a combined move played
				for (int i = 0; i < moves.Count; i++)
				{
					var move = moves[i];
					// the given from/to move was a single move, therefore we can return the move right away
					if (move.From == from && move.To == to)
					{
						playedMoves.Add(move);
						break;
					}
					// the from/to move was a combined one, therefore we need to return multiple moves in order to properly play it
					// but exclude always the first move in the sequence and check if the moves before actually were chained together
					else if (move.To == to && i != 0 && moves.Take(new Range(0, i)).Any(m => m.From == from))
					{
						playedMoves.AddRange(moves.Take(new Range(0, i + 1)));
						break;
					}
				}

				// we found our moves based on the given from/to
				if (playedMoves.Count > 0)
				{
					break;
				}
			}

			// we need to strip down the remaining possible move sequences
			// we strip out the already made move from the remaining sequences
			if (potentialMoveSequences.Count > 0)
			{
				foreach (var ms in potentialMoveSequences)
				{
					foreach (var playedMove in playedMoves)
					{
						ms.Moves.Remove(playedMove);
					}
					var unplayableMoves = ms.Moves.Where(m => !remainingRolls.Contains(DiceRolls.GetMoveDistance(model, m.From, m.To, out var bearOff)) && !bearOff).ToList();
					foreach (var toRemove in unplayableMoves)
					{
						ms.Moves.Remove(toRemove);
					}
				}
				var strippedMoveSequences = potentialMoveSequences.Where(pms => pms.Moves.Count > 0);
				Clear();
				AddRange(strippedMoveSequences);
			}

			return playedMoves.Count > 0;
		}
	}
}
