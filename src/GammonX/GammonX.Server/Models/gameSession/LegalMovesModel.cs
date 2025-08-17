using GammonX.Server.Contracts;

namespace GammonX.Server.Models
{
	/// <summary>
	/// 
	/// </summary>
	public class LegalMovesModel
	{
		/// <summary>
		/// 
		/// </summary>
		public LegalMoveContract[] LegalMoves { get; private set; }

		public LegalMovesModel(LegalMoveContract[] legalMoves)
		{
			LegalMoves = legalMoves;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <exception cref="InvalidOperationException"></exception>
		public void UseDice(int from, int to)
		{
			var usedLegalMove = LegalMoves.FirstOrDefault(r => r.From == from && r.To == to);
			if (usedLegalMove != null)
			{
				usedLegalMove.Use();
			}

			throw new InvalidOperationException($"No unused legal move from '{from}' to '{to}' left");
		}
	}
}
