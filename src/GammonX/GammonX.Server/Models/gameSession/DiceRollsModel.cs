using GammonX.Server.Contracts;

namespace GammonX.Server.Models
{
	/// <summary>
	/// 
	/// </summary>
	
	public class DiceRollsModel
	{
		/// <summary>
		/// 
		/// </summary>
		public DiceRollContract[] DiceRolls { get; private set; }

		public DiceRollsModel(DiceRollContract[] diceRolls) 
		{
			DiceRolls = diceRolls;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="diceRoll"></param>
		/// <exception cref="InvalidOperationException"></exception>
		public void UseDice(int diceRoll)
		{
			var usedDiceRoll = DiceRolls.FirstOrDefault(r => r.Roll == diceRoll && !r.Used);
			if (usedDiceRoll != null)
			{
				usedDiceRoll.Use();
			}

			throw new InvalidOperationException($"No unused dice roll with a value of '{diceRoll}' left");
		}
	}
}
