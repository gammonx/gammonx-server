using System.Collections;

namespace GammonX.Engine.Tests.Data
{
	/// <summary>
	/// Represents the expected legal moves for a start board for backgammon, tavla and portes.
	/// </summary>
	internal class BackgammonStartBoardLegalMovesForWhiteTestData : IEnumerable<object[]>
	{
		public IEnumerator<object[]> GetEnumerator()
		{
			yield return new object[]
				{
					1,
					2,
					true,
					new ValueTuple<int, int>[]
					{
						(0, 1),
						(0, 2),
						(0, 3),
						(11, 13),
						(11, 14),
						(16, 17),
						(16, 18),
						(16, 19),
						(18, 19),
						(18, 20),
						(18, 21)
					}
				};
			yield return new object[]
				{
					1,
					3,
					true,
					new ValueTuple<int, int>[]
					{
						(0, 1),
						(0, 3),
						(0, 4),
						(11, 14),
						(11, 15),
						(16, 17),
						(16, 19),
						(16, 20),
						(18, 19),
						(18, 21),
						(18, 22)
					}
				};
			yield return new object[]
				{
					1,
					4,
					true,
					new ValueTuple<int, int>[]
					{
						(0, 1),
						(0, 4),
						(11, 15),
						(11, 16),
						(16, 17),
						(16, 20),
						(16, 21),
						(18, 19),
						(18, 22)
					}
				};
			yield return new object[]
				{
					1,
					5,
					true,
					new ValueTuple<int, int>[]
					{
						(0, 1),
						(0, 6),
						(11, 16),
						(11, 17),
						(16, 17),
						(16, 21),
						(16, 22),
						(18, 19)
					}
				};
			yield return new object[]
				{
					1,
					6,
					true,
					new ValueTuple<int, int>[]
					{
						(0, 1),
						(0, 6),
						(11, 17),
						(11, 18),
						(16, 17),
						(16, 22),
						(18, 19)
					}
				};
			yield return new object[]
				{
					2,
					3,
					true,
					new ValueTuple<int, int>[]
					{
						(0, 2),
						(0, 3),
						(11, 13),
						(11, 14),
						(11, 16),
						(16, 18),
						(16, 19),
						(16, 21),
						(18, 20),
						(18, 21)
					}
				};
			yield return new object[]
				{
					2,
					4,
					true,
					new ValueTuple<int, int>[]
					{
						(0, 2),
						(0, 4),
						(0, 6),
						(11, 13),
						(11, 15),
						(11, 17),
						(16, 18),
						(16, 20),
						(16, 22),
						(18, 20),
						(18, 22)
					}
				};
			yield return new object[]
				{
					2,
					5,
					true,
					new ValueTuple<int, int>[]
					{
						(0, 2),
						(11, 13),
						(11, 16),
						(11, 18),
						(16, 18),
						(16, 21),
						(18, 20)
					}
				};
			yield return new object[]
				{
					2,
					6,
					true,
					new ValueTuple<int, int>[]
					{
						(0, 2),
						(0, 6),
						(0, 8),
						(11, 13),
						(11, 17),
						(11, 19),
						(16, 18),
						(16, 22),
						(18, 20)
					}
				};
			yield return new object[]
				{
					3,
					4,
					true,
					new ValueTuple<int, int>[]
					{
						(0, 3),
						(0, 4),
						(11, 14),
						(11, 15),
						(11, 18),
						(16, 19),
						(16, 20),
						(18, 21),
						(18, 22)
					}
				};
			yield return new object[]
				{
					3,
					5,
					true,
					new ValueTuple<int, int>[]
					{
						(0, 3),
						(0, 8),
						(11, 14),
						(11, 16),
						(11, 19),
						(16, 19),
						(16, 21),
						(18, 21)
					}
				};
			yield return new object[]
				{
					3,
					6,
					true,
					new ValueTuple<int, int>[]
					{
						(0, 3),
						(0, 6),
						(0, 9),
						(11, 14),
						(11, 17),
						(11, 20),
						(16, 19),
						(16, 22),
						(18, 21)
					}
				};
			yield return new object[]
				{
					4,
					5,
					true,
					new ValueTuple<int, int>[]
					{
						(0, 4),
						(0, 9),
						(11, 15),
						(11, 16),
						(11, 20),
						(16, 20),
						(16, 21),
						(18, 22)
					}
				};
			yield return new object[]
				{
					4,
					6,
					true,
					new ValueTuple<int, int>[]
					{
						(0, 4),
						(0, 6),
						(0, 10),
						(11, 15),
						(11, 17),
						(11, 21),
						(16, 20),
						(16, 22),
						(18, 22)
					}
				};
			yield return new object[]
				{
					5,
					6,
					true,
					new ValueTuple<int, int>[]
					{
						(0, 6),
						(0, 11),
						(11, 16),
						(11, 17),
						(11, 22),
						(16, 21),
						(16, 22)
					}
				};
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	/// <summary>
	/// Represents the expected legal moves for a start board for backgammon, tavla and portes.
	/// </summary>
	internal class BackgammonStartBoardLegalMovesForBlackTestData : IEnumerable<object[]>
	{
		public IEnumerator<object[]> GetEnumerator()
		{
			yield return new object[]
			{
			1,
			2,
			false,
			new ValueTuple<int, int>[]
			{
				(5, 4),
				(5, 3),
				(5, 2),
				(7, 6),
				(7, 5),
				(7, 4),
				(12, 10),
				(12, 9),
				(23, 22),
				(23, 21),
				(23, 20)
			}
			};
			yield return new object[]
			{
			1,
			3,
			false,
			new ValueTuple<int, int>[]
			{
				(5, 4),
				(5, 2),
				(5, 1),
				(7, 6),
				(7, 4),
				(7, 3),
				(12, 9),
				(12, 8),
				(23, 22),
				(23, 20),
				(23, 19)
			}
			};
			yield return new object[]
			{
			1,
			4,
			false,
			new ValueTuple<int, int>[]
			{
				(5, 4),
				(5, 1),
				(7, 6),
				(7, 3),
				(7, 2),
				(12, 8),
				(12, 7),
				(23, 22),
				(23, 19)
			}
			};
			yield return new object[]
			{
			1,
			5,
			false,
			new ValueTuple<int, int>[]
			{
				(5, 4),
				(7, 6),
				(7, 2),
				(7, 1),
				(12, 7),
				(12, 6),
				(23, 22),
				(23, 17)
			}
			};
			yield return new object[]
			{
			1,
			6,
			false,
			new ValueTuple<int, int>[]
			{
				(5, 4),
				(7, 6),
				(7, 1),
				(12, 6),
				(12, 5),
				(23, 22),
				(23, 17)
			}
			};
			yield return new object[]
			{
			2,
			3,
			false,
			new ValueTuple<int, int>[]
			{
				(5, 3),
				(5, 2),
				(7, 5),
				(7, 4),
				(7, 2),
				(12, 10),
				(12, 9),
				(12, 7),
				(23, 21),
				(23, 20)
			}
			};
			yield return new object[]
			{
			2,
			4,
			false,
			new ValueTuple<int, int>[]
			{
				(5, 3),
				(5, 1),
				(7, 5),
				(7, 3),
				(7, 1),
				(12, 10),
				(12, 8),
				(12, 6),
				(23, 21),
				(23, 19),
				(23, 17)
			}
			};
			yield return new object[]
			{
			2,
			5,
			false,
			new ValueTuple<int, int>[]
			{
				(5, 3),
				(7, 5),
				(7, 2),
				(12, 10),
				(12, 7),
				(12, 5),
				(23, 21),
			}
			};
			yield return new object[]
			{
			2,
			6,
			false,
			new ValueTuple<int, int>[]
			{
				(5, 3),
				(7, 5),
				(7, 1),
				(12, 10),
				(12, 6),
				(12, 4),
				(23, 21),
				(23, 17),
				(23, 15)
			}
			};
			yield return new object[]
			{
			3,
			4,
			false,
			new ValueTuple<int, int>[]
			{
				(5, 2),
				(5, 1),
				(7, 4),
				(7, 3),
				(12, 9),
				(12, 8),
				(12, 5),
				(23, 20),
				(23, 19)
			}
			};
			yield return new object[]
			{
			3,
			5,
			false,
			new ValueTuple<int, int>[]
			{
				(5, 2),
				(7, 4),
				(7, 2),
				(12, 9),
				(12, 7),
				(12, 4),
				(23, 20),
				(23, 15)
			}
			};
			yield return new object[]
			{
			3,
			6,
			false,
			new ValueTuple<int, int>[]
			{
				(5, 2),
				(7, 4),
				(7, 1),
				(12, 9),
				(12, 6),
				(12, 3),
				(23, 20),
				(23, 17),
				(23, 14)
			}
			};
			yield return new object[]
			{
			4,
			5,
			false,
			new ValueTuple<int, int>[]
			{
				(5, 1),
				(7, 3),
				(7, 2),
				(12, 8),
				(12, 7),
				(12, 3),
				(23, 19),
				(23, 14)
			}
			};
			yield return new object[]
			{
			4,
			6,
			false,
			new ValueTuple<int, int>[]
			{
				(5, 1),
				(7, 3),
				(7, 1),
				(12, 8),
				(12, 6),
				(12, 2),
				(23, 19),
				(23, 17),
				(23, 13)
			}
			};
			yield return new object[]
			{
			5,
			6,
			false,
			new ValueTuple<int, int>[]
			{
				(7, 2),
				(7, 1),
				(12, 7),
				(12, 6),
				(12, 1),
				(23, 17),
				(23, 12)
			}
			};
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	/// <summary>
	/// Represents the expected legal moves for all double rolls (Pasch) for black on the backgammon start board.
	/// </summary>
	internal class BackgammonStartBoardDoubleRollsForBlackTestData : IEnumerable<object[]>
	{
		public IEnumerator<object[]> GetEnumerator()
		{
			yield return new object[]
			{
			1,
			1,
			1,
			1,
			false,
			new ValueTuple<int, int>[]
			{
				(5, 4),
				(5, 3),
				(5, 2),
				(5, 1),
				(7, 6),
				(7, 5),
				(7, 4),
				(7, 3),
				(23, 22),
				(23, 21),
				(23, 20),
				(23, 19)
			}
			};
			yield return new object[]
			{
			2,
			2,
			2,
			2,
			false,
			new ValueTuple<int, int>[]
			{
				(5, 3),
				(5, 1),
				(7, 5),
				(7, 3),
				(7, 1),
				(12, 10),
				(12, 8),
				(12, 6),
				(12, 4),
				(23, 21),
				(23, 19),
				(23, 17),
				(23, 15)
			}
			};
			yield return new object[]
			{
			3,
			3,
			3,
			3,
			false,
			new ValueTuple<int, int>[]
			{
				(5, 2),
				(7, 4),
				(7, 1),
				(12, 9),
				(12, 6),
				(12, 3),
				(23, 20),
				(23, 17),
				(23, 14)
			}
			};
			yield return new object[]
			{
			4,
			4,
			4,
			4,
			false,
			new ValueTuple<int, int>[]
			{
				(5, 1),
				(7, 3),
				(12, 8),
				(12, 4),
				(23, 19),
				(23, 15)
			}
			};
			yield return new object[]
			{
			5,
			5,
			5,
			5,
			false,
			new ValueTuple<int, int>[]
			{
				(7, 2),
				(12, 7),
				(12, 2)
			}
			};
			yield return new object[]
			{
			6,
			6,
			6,
			6,
			false,
			new ValueTuple<int, int>[]
			{
				(7, 1),
				(12, 6),
				(23, 17)
			}
			};
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	/// <summary>
	/// Represents the expected legal moves for all double rolls (Pasch) for white on the backgammon start board.
	/// </summary>
	internal class BackgammonStartBoardDoubleRollsForWhiteTestData : IEnumerable<object[]>
	{
		public IEnumerator<object[]> GetEnumerator()
		{
			yield return new object[]
			{
			1,
			1,
			1,
			1,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 1),
				(0, 2),
				(0, 3),
				(0, 4),
				(16, 17),
				(16, 18),
				(16, 19),
				(16, 20),
				(18, 19),
				(18, 20),
				(18, 21),
				(18, 22)
			}
			};
			yield return new object[]
			{
			2,
			2,
			2,
			2,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 2),
				(0, 4),
				(0, 6),
				(0, 8),
				(11, 13),
				(11, 15),
				(11, 17),
				(11, 19),
				(16, 18),
				(16, 20),
				(16, 22),
				(18, 20),
				(18, 22)
			}
			};
			yield return new object[]
			{
			3,
			3,
			3,
			3,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 3),
				(0, 6),
				(0, 9),
				(11, 14),
				(11, 17),
				(11, 20),
				(16, 19),
				(16, 22),
				(18, 21)
			}
			};
			yield return new object[]
			{
			4,
			4,
			4,
			4,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 4),
				(0, 8),
				(11, 15),
				(11, 19),
				(16, 20),
				(18, 22)
			}
			};
			yield return new object[]
			{
			5,
			5,
			5,
			5,
			true,
			new ValueTuple<int, int>[]
			{
				(11, 16),
				(11, 21),
				(16, 21)
			}
			};
			yield return new object[]
			{
			6,
			6,
			6,
			6,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 6),
				(11, 17),
				(16, 22)
			}
			};
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	/// <summary>
	/// Represents the expected legal moves for all regular rolls for white on the fevga start board.
	/// </summary>
	internal class FevgaPlakotoStartBoardLegalMovesForWhiteTestData : IEnumerable<object[]>
	{
		public IEnumerator<object[]> GetEnumerator()
		{
			yield return new object[]
			{
			1,
			2,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 1),
				(0, 2),
				(0, 3),
			}
			};
			yield return new object[]
			{
			1,
			3,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 1),
				(0, 3),
				(0, 4),
			}
			};
			yield return new object[]
			{
			1,
			4,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 1),
				(0, 4),
				(0, 5),
			}
			};
			yield return new object[]
			{
			1,
			5,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 1),
				(0, 5),
				(0, 6),
			}
			};
			yield return new object[]
			{
			1,
			6,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 1),
				(0, 6),
				(0, 7),
			}
			};
			yield return new object[]
			{
			2,
			3,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 2),
				(0, 3),
				(0, 5),
			}
			};
			yield return new object[]
			{
			2,
			4,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 2),
				(0, 4),
				(0, 6),
			}
			};
			yield return new object[]
			{
			2,
			5,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 2),
				(0, 5),
				(0, 7),
			}
			};
			yield return new object[]
			{
			2,
			6,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 2),
				(0, 6),
				(0, 8),
			}
			};
			yield return new object[]
			{
			3,
			4,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 3),
				(0, 4),
				(0, 7),
			}
			};
			yield return new object[]
			{
			3,
			5,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 3),
				(0, 5),
				(0, 8),
			}
			};
			yield return new object[]
			{
			3,
			6,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 3),
				(0, 6),
				(0, 9),
			}
			};
			yield return new object[]
			{
			4,
			5,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 4),
				(0, 5),
				(0, 9),
			}
			};
			yield return new object[]
			{
			4,
			6,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 4),
				(0, 6),
				(0, 10),
			}
			};
			yield return new object[]
			{
			5,
			6,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 5),
				(0, 6),
				(0, 11),
			}
			};
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	/// <summary>
	/// Represents the expected legal moves for all double rolls (Pasch) for white on the fevga start board.
	/// </summary>
	internal class FevgaPlakotoStartBoardLegalMovesDoubleRollsForWhiteTestData : IEnumerable<object[]>
	{
		public IEnumerator<object[]> GetEnumerator()
		{
			yield return new object[]
			{
			1,
			1,
			1,
			1,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 1),
				(0, 2),
				(0, 3),
				(0, 4),
			}
			};
			yield return new object[]
			{
			2,
			2,
			2,
			2,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 2),
				(0, 4),
				(0, 6),
				(0, 8),
			}
			};
			yield return new object[]
			{
			3,
			3,
			3,
			3,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 3),
				(0, 6),
				(0, 9),
				(0, 12)
			}
			};
			yield return new object[]
			{
			4,
			4,
			4,
			4,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 4),
				(0, 8),
				(0, 12),
				(0, 16)
			}
			};
			yield return new object[]
			{
			5,
			5,
			5,
			5,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 5),
				(0, 10),
				(0, 15),
				(0, 20),
			}
			};
			yield return new object[]
			{
			6,
			6,
			6,
			6,
			true,
			new ValueTuple<int, int>[]
			{
				(0, 6),
				(0, 12),
				(0, 18),
			}
			};
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	/// <summary>
	/// Represents the expected legal moves for all regular rolls for black on the fevga and plakoto start board.
	/// </summary>
	internal class FevgaStartBoardLegalMovesForBlackTestData : IEnumerable<object[]>
	{
		public IEnumerator<object[]> GetEnumerator()
		{
			yield return new object[] { 1, 2, false, new (int, int)[] { (12, 13), (12, 14) } };
			yield return new object[] { 1, 3, false, new (int, int)[] { (12, 13), (12, 15) } };
			yield return new object[] { 1, 4, false, new (int, int)[] { (12, 13), (12, 16) } };
			yield return new object[] { 1, 5, false, new (int, int)[] { (12, 13), (12, 17) } };
			yield return new object[] { 1, 6, false, new (int, int)[] { (12, 13), (12, 18) } };
			yield return new object[] { 2, 3, false, new (int, int)[] { (12, 14), (12, 15) } };
			yield return new object[] { 2, 4, false, new (int, int)[] { (12, 14), (12, 16) } };
			yield return new object[] { 2, 5, false, new (int, int)[] { (12, 14), (12, 17) } };
			yield return new object[] { 2, 6, false, new (int, int)[] { (12, 14), (12, 18) } };
			yield return new object[] { 3, 4, false, new (int, int)[] { (12, 15), (12, 16) } };
			yield return new object[] { 3, 5, false, new (int, int)[] { (12, 15), (12, 17) } };
			yield return new object[] { 3, 6, false, new (int, int)[] { (12, 15), (12, 18) } };
			yield return new object[] { 4, 5, false, new (int, int)[] { (12, 16), (12, 17) } };
			yield return new object[] { 4, 6, false, new (int, int)[] { (12, 16), (12, 18) } };
			yield return new object[] { 5, 6, false, new (int, int)[] { (12, 17), (12, 18) } };
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	/// <summary>
	/// Represents the expected legal moves for all double rolls (Pasch) for black on the fevga and plakoto start board.
	/// </summary>
	internal class FevgaStartBoardLegalMovesForBlackDoublesTestData : IEnumerable<object[]>
	{
		public IEnumerator<object[]> GetEnumerator()
		{
			yield return new object[] { 1, 1, 1, 1, false, new (int, int)[] { (12, 13) } };
			yield return new object[] { 2, 2, 2, 2, false, new (int, int)[] { (12, 14) } };
			yield return new object[] { 3, 3, 3, 3, false, new (int, int)[] { (12, 15) } };
			yield return new object[] { 4, 4, 4, 4, false, new (int, int)[] { (12, 16) } };
			yield return new object[] { 5, 5, 5, 5, false, new (int, int)[] { (12, 17) } };
			yield return new object[] { 6, 6, 6, 6, false, new (int, int)[] { (12, 18) } };
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	/// <summary>
	/// Represents the expected legal moves for all regular rolls for black on the plakoto start board.
	/// </summary>
	internal class PlakotoStartBoardLegalMovesForBlackTestData : IEnumerable<object[]>
	{
		public IEnumerator<object[]> GetEnumerator()
		{
			yield return new object[] { 1, 2, false, new (int, int)[] { (23, 22), (23, 21), (23, 20) } };
			yield return new object[] { 1, 3, false, new (int, int)[] { (23, 22), (23, 20), (23, 19) } };
			yield return new object[] { 1, 4, false, new (int, int)[] { (23, 22), (23, 19), (23, 18) } };
			yield return new object[] { 1, 5, false, new (int, int)[] { (23, 22), (23, 18), (23, 17) } };
			yield return new object[] { 1, 6, false, new (int, int)[] { (23, 22), (23, 17), (23, 16) } };
			yield return new object[] { 2, 3, false, new (int, int)[] { (23, 21), (23, 20), (23, 18) } };
			yield return new object[] { 2, 4, false, new (int, int)[] { (23, 21), (23, 19), (23, 17) } };
			yield return new object[] { 2, 5, false, new (int, int)[] { (23, 21), (23, 18), (23, 16) } };
			yield return new object[] { 2, 6, false, new (int, int)[] { (23, 21), (23, 17), (23, 15) } };
			yield return new object[] { 3, 4, false, new (int, int)[] { (23, 20), (23, 19), (23, 16) } };
			yield return new object[] { 3, 5, false, new (int, int)[] { (23, 20), (23, 18), (23, 15) } };
			yield return new object[] { 3, 6, false, new (int, int)[] { (23, 20), (23, 17), (23, 14) } };
			yield return new object[] { 4, 5, false, new (int, int)[] { (23, 19), (23, 18), (23, 14) } };
			yield return new object[] { 4, 6, false, new (int, int)[] { (23, 19), (23, 17), (23, 13) } };
			yield return new object[] { 5, 6, false, new (int, int)[] { (23, 18), (23, 17), (23, 12) } };
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	/// <summary>
	/// Represents the expected legal moves for all double rolls (Pasch) for black on the plakoto start board.
	/// </summary>
	internal class PlakotoStartBoardLegalMovesForBlackDoublesTestData : IEnumerable<object[]>
	{
		public IEnumerator<object[]> GetEnumerator()
		{
			yield return new object[] { 1, 1, 1, 1, false, new (int, int)[] { (23, 22), (23, 21), (23, 20), (23, 19) } };
			yield return new object[] { 2, 2, 2, 2, false, new (int, int)[] { (23, 21), (23, 19), (23, 17), (23, 15) } };
			yield return new object[] { 3, 3, 3, 3, false, new (int, int)[] { (23, 20), (23, 17), (23, 14), (23, 11) } };
			yield return new object[] { 4, 4, 4, 4, false, new (int, int)[] { (23, 19), (23, 15), (23, 11), (23, 7) } };
			yield return new object[] { 5, 5, 5, 5, false, new (int, int)[] { (23, 18), (23, 13), (23, 8), (23, 3) } };
			yield return new object[] { 6, 6, 6, 6, false, new (int, int)[] { (23, 17), (23, 11), (23, 5) } };
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
