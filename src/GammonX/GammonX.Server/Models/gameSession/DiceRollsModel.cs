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

		/// <summary>
		/// 
		/// </summary>
		public bool HasUnused => DiceRolls.Any(dr => !dr.Used);

		public DiceRollsModel(params DiceRollContract[] diceRolls) 
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

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public DiceRollContract[] GetUnusedDiceRolls()
		{
			return DiceRolls.Where(dr => !dr.Used).ToArray();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public int[] GetUnusedDiceRollValues()
		{
			return DiceRolls.Where(dr => !dr.Used).Select(dr => dr.Roll).ToArray();
		}
	}
}
