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
		public List<MoveModel> Moves { get; } = new();

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
			return other != null && SequenceKey() == other.SequenceKey();
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
			return obj.SequenceKey().GetHashCode();
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

		// <inheritdoc />
		public bool Equals(MoveModel? other)
		{
			return other != null && other.From == From && other.To == To;
		}
	}
}
