using GammonX.Server.Contracts;

namespace GammonX.Server.Models
{
	public class LegalMovesModel
	{
		public LegalMoveContract[] LegalMoves { get; private set; }

		public LegalMovesModel(LegalMoveContract[] legalMoves)
		{
			LegalMoves = legalMoves;
		}

		/// <summary>
		/// Uses a legal move from the list of legal moves.
		/// </summary>
		/// <param name="from">From board array index.</param>
		/// <param name="to">To board array index.</param>
		/// <exception cref="InvalidOperationException">Throws if the given legal move is unknown.</exception>
		public void UseLegalMove(int from, int to)
		{
			var usedLegalMove = LegalMoves.FirstOrDefault(r => r.From == from && r.To == to);
			if (usedLegalMove != null)
			{
				usedLegalMove.Use();
			}

			throw new InvalidOperationException($"No unused legal move from '{from}' to '{to}' left");
		}

		public bool HasLegalMoves()
		{
			return LegalMoves.Any(lm => !lm.Used);
		}
	}
}
