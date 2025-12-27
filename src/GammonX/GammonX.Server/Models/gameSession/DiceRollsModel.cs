using GammonX.Engine.Models;

using GammonX.Models.Enums;

using GammonX.Server.Contracts;
using Serilog;

namespace GammonX.Server.Models
{	
	public class DiceRollsModel : List<DiceRollContract>
	{
		public bool HasUnused => this.Any(dr => !dr.Used);

		public DiceRollsModel() 
		{
			// pass
		}
				
		public bool TryUseDice(IBoardModel model, int from, int to)
		{
			var moveDistance = GetMoveDistance(model, from, to, out var bearOffMove);
			var unused = GetUnusedDiceRolls();
			var result = TryFindDiceUsage(unused, moveDistance, bearOffMove, out var usedDices);
			if (usedDices != null)
			{
				usedDices.ForEach(dice => dice.Use());
				return result;
			}
			Log.Error($"An error occurred while determining the used dices for move '{from}' > '{to}'");
			return false;
		}

		public void UndoDiceRoll(int roll)
		{
			var diceRoll = this.FirstOrDefault(dr => dr.Used && dr.Roll == roll);
			if (diceRoll != null) 
			{
				diceRoll.Used = false;
				return;
			}
			throw new InvalidOperationException("An error occurred while undoing a dice roll");
		}

		public DiceRollContract[] GetUnusedDiceRolls()
		{
			return this.Where(dr => !dr.Used).ToArray();
		}

		public int[] GetRemainingRolls()
		{
			return this.Where(dr => !dr.Used).Select(dr => dr.Roll).ToArray();
		}

		#region Private Members

		private static bool TryFindDiceUsage(
			DiceRollContract[] unusedDices, 
			int distance, 
			bool bearOffMove, 
			out List<DiceRollContract> usedDices)
		{
			usedDices = new List<DiceRollContract>();
			// we order the rolls in ascending order to make sure that the lowest roll
			// always bears of a potential checker
			unusedDices = unusedDices.OrderBy(dice => dice.Roll).ToArray();
			return Backtrack(unusedDices, distance, 0, new List<DiceRollContract>(), bearOffMove, out usedDices);
		}

		private static bool Backtrack(
			DiceRollContract[] dices,
			int distance,
			int start,
			List<DiceRollContract> current,
			bool bearOffMove,
			out List<DiceRollContract> result)
		{
			// base case: distance fully covered
			if (distance <= 0)
			{
				result = new List<DiceRollContract>(current);
				return true;
			}

			for (int i = start; i < dices.Length; i++)
			{
				if (bearOffMove)
				{
					// enforce exact die usage first
					var exact = dices.FirstOrDefault(d => !current.Contains(d) && d.Roll == distance);
					if (exact != null)
					{
						result = new List<DiceRollContract> { exact };
						return true;
					}

					// if no exact, use smallest die >= distance
					var higher = dices.FirstOrDefault(d => !current.Contains(d) && d.Roll > distance);
					if (higher != null)
					{
						result = new List<DiceRollContract> { higher };
						return true;
					}

					// fallback: try using this dice in combination
					current.Add(dices[i]);
					if (Backtrack(dices.Where((_, idx) => idx != i).ToArray(), distance - dices[i].Roll, 0, current, bearOffMove, out result))
					{
						result.AddRange(current);
                        return true;
                    }
					current.RemoveAt(current.Count - 1);
				}
				else
				{
					if (dices[i].Roll <= distance && !current.Contains(dices[i]))
					{
						current.Add(dices[i]);
						if (Backtrack(dices.Where((_, idx) => idx != i).ToArray(), distance - dices[i].Roll, 0, current, bearOffMove, out result))
							return true;
						current.RemoveAt(current.Count - 1);
					}
				}
			}

			result = null!;
			return false;
		}

		public static int GetMoveDistance(IBoardModel model, int from, int to, out bool bearOffMove)
		{
			if (to == BoardPositions.BearOffWhite)
			{
				var distance = Math.Abs(from - (model.HomeRangeWhite.End.Value + 1));
				bearOffMove = true;
				return distance;
			}
			else if (to == BoardPositions.BearOffBlack)
			{
				// black checkers in fevga move same direction as white in others
				if (model.Modus == GameModus.Fevga)
				{
					var distance = Math.Abs(from - (model.HomeRangeBlack.End.Value + 1));
					bearOffMove = true;
					return distance;
				}
				else
				{
					var distance = Math.Abs(from + 1 - model.HomeRangeBlack.End.Value);
					bearOffMove = true;
					return distance;
				}
			}
			// black checkers in fevga mode start from index 12
			else if (from == BoardPositions.HomeBarBlack && model.Modus == GameModus.Fevga)
			{
				var distance = Math.Abs(model.StartRangeBlack.Start.Value - 1 - to);
				bearOffMove = false;
				return distance;
			}
			// black checkers in fevga move from 12 > 23 > 0 > 11
			else if (from > to && model.Modus == GameModus.Fevga)
			{
				var size = model.Fields.Length;
				var distance = (to - from + size) % size;
				bearOffMove = false;
				return distance;
			}
			else
			{
				var distance = Math.Abs(from - to);
				bearOffMove = false;
				return distance;
			}
		}

		#endregion Private Members
	}
}
