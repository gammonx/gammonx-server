using GammonX.Server.Contracts;

namespace GammonX.Server.Models
{	
	public class DiceRollsModel
	{
		public DiceRollContract[] DiceRolls { get; private set; }

		public bool HasUnused => DiceRolls.Any(dr => !dr.Used);

		public DiceRollsModel(params DiceRollContract[] diceRolls) 
		{
			DiceRolls = diceRolls;
		}

		/// <summary>
		/// Marks the given dice roll as used.
		/// </summary>
		/// <param name="diceRoll">Dice roll value.</param>
		/// <exception cref="InvalidOperationException">If not part of the known dice rolls.</exception>
		public void UseDice(int diceRoll)
		{
			var usedDiceRoll = DiceRolls.FirstOrDefault(r => r.Roll == diceRoll && !r.Used);
			if (usedDiceRoll != null)
			{
				usedDiceRoll.Use();
			}

			throw new InvalidOperationException($"No unused dice roll with a value of '{diceRoll}' left");
		}

		public DiceRollContract[] GetUnusedDiceRolls()
		{
			return DiceRolls.Where(dr => !dr.Used).ToArray();
		}

		public int[] GetUnusedDiceRollValues()
		{
			return DiceRolls.Where(dr => !dr.Used).Select(dr => dr.Roll).ToArray();
		}
	}
}
