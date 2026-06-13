using GammonX.Engine.Services;

using GammonX.Models.Enums;

using System.Diagnostics;
using System.Runtime.Serialization;

namespace GammonX.Engine.Models
{
	/// <summary>
	/// A move sequence represents a set of plays (from/to) which can be made with a set of dices.
	/// </summary>
	[DataContract]
	public class MoveSequenceModel : IEquatable<MoveSequenceModel>
	{
		/// <summary>
		/// Gets a list of plays. Contains 4 moves for a pasch and 2 moves for 2 different dice values.
		/// </summary>
		[DataMember(Name = "moves")]
		public List<MoveModel> Moves { get; set; } = new();

		/// <summary>
		/// Gets a list of used dices in order to execute <see cref="Moves"/>.
		/// </summary>
		[IgnoreDataMember]
		public List<int> UsedDices { get; } = new();

		public string SequenceKey()
		{
			var movesPart = string.Join(";", Moves.Select(m => $"{m.From}->{m.To}"));
			return movesPart;
		}

		public MoveSequenceModel DeepClone()
		{
			var moves = Moves.Select(m => new MoveModel(m.From, m.To)).ToList();
			var seqModel = new MoveSequenceModel();
			seqModel.Moves.AddRange(moves);
			seqModel.UsedDices.AddRange(UsedDices);
			return seqModel;
		}

		// <inheritdoc />
		public bool Equals(MoveSequenceModel? other)
		{
			if (other == null || Moves.Count != other.Moves.Count)
				return false;
			for (int i = 0; i < Moves.Count; i++)
			{
				if (Moves[i].From != other.Moves[i].From || Moves[i].To != other.Moves[i].To)
					return false;
			}
			return true;
		}

		public MoveSequenceModel Invert(GameModus modus)
		{
			var invertedMoves = Moves.Select(m => m.Invert(modus)).ToList();
			var seqModel = new MoveSequenceModel();
			seqModel.Moves.AddRange(invertedMoves);
			seqModel.UsedDices.AddRange(UsedDices);
			return seqModel;
		}
	}

	// <inheritdoc />
	public class MoveSequenceModelComparer : IEqualityComparer<MoveSequenceModel>
	{
		// <inheritdoc />
		public bool Equals(MoveSequenceModel? x, MoveSequenceModel? y)
		{
			if (x == null && y == null) return true;
			if (x == null || y == null) return false;
			return x.Equals(y);
		}

		// <inheritdoc />
		public int GetHashCode(MoveSequenceModel obj)
		{
			var hash = new HashCode();
			foreach (var move in obj.Moves)
			{
				hash.Add(move.From);
				hash.Add(move.To);
			}
			return hash.ToHashCode();
		}
	}

	/// <summary>
	/// A move represents a checker moved from index to index.
	/// </summary>
	[DataContract]
	[DebuggerDisplay("{From}/{To}")]
	public class MoveModel : IEquatable<MoveModel>
	{
		/// <summary>
		/// Gets the from index.
		/// </summary>
		[DataMember(Name = "from")]
		public int From { get; set; } = new();

		/// <summary>
		/// Gets the to index.
		/// </summary>
		[DataMember(Name = "to")]
		public int To { get; set; } = new();

		public MoveModel(int from, int to)
		{
			From = from;
			To = to;
		}

		public MoveModel Invert(GameModus modus)
		{
			if (modus == GameModus.Fevga)
			{
				var inverted = BoardBroker.InvertBoardMoveDiagonalHorizontally(From, To);
				return new MoveModel(inverted.Item1, inverted.Item2);
			}
			else
			{
				var inverted = BoardBroker.InvertBoardMoveHorizontally(From, To);
				return new MoveModel(inverted.Item1, inverted.Item2);
			}
		}

        // <inheritdoc />
        public bool Equals(MoveModel? other)
        {
            return other != null && other.From == From && other.To == To;
        }

        // <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as MoveModel);
        }

        // <inheritdoc />
        public override int GetHashCode()
        {
			return base.GetHashCode();
        }
    }
}
