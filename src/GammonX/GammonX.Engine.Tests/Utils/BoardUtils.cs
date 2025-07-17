using GammonX.Engine.Models;

namespace GammonX.Engine.Tests.Utils
{
	internal static class BoardUtils
	{
		public static void SetFields(this IBoardModel board, int[] fields)
		{
			if (board is BoardBaseImpl boardBase)
			{
				boardBase.SetFields(fields);
			}
			else
			{
				throw new InvalidOperationException("SetFields can only be used on BoardBaseImpl instances.");
			}
		}
	}
}
