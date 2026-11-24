using GammonX.Engine.Models;

using GammonX.Models.Enums;

using GammonX.Server.Models;

namespace GammonX.Server.Tests
{
    public class MoveSequencesTests
    {
        private static MoveModel M(int from, int to) => new(from, to);

        private static MoveSequenceModel Seq(params MoveModel[] moves)
        {
            var s = new MoveSequenceModel();
            s.Moves.AddRange(moves);
            return s;
        }

        [Theory]
        [InlineData(0, 5)]
        [InlineData(BoardPositions.HomeBarWhite, 0)]
        [InlineData(23, BoardPositions.BearOffWhite)]
        [InlineData(BoardPositions.HomeBarBlack, 23)]
        public void MoveModelClonePreservesPositionsIndependently(int from, int to)
        {
            var move = M(from, to);

            var clone = Assert.IsType<MoveModel>(((ICloneable)move).Clone());

            Assert.NotSame(move, clone);
            Assert.Equal(from, clone.From);
            Assert.Equal(to, clone.To);
            clone.From = 99;
            clone.To = 98;
            Assert.Equal(from, move.From);
            Assert.Equal(to, move.To);
            move.To = 97;
            Assert.Equal(98, clone.To);
        }

        [Fact]
        public void MoveSequenceDeepCloneCopiesMovesAndUsedDice()
        {
            var sequence = Seq(M(0, 2), M(2, 5));
            sequence.UsedDices.AddRange([2, 3]);

            var clone = sequence.DeepClone();

            Assert.NotSame(sequence, clone);
            Assert.NotSame(sequence.Moves, clone.Moves);
            Assert.NotSame(sequence.UsedDices, clone.UsedDices);
            Assert.Equal(sequence.Moves, clone.Moves);
            Assert.Equal(sequence.UsedDices, clone.UsedDices);
            for (var moveIndex = 0; moveIndex < sequence.Moves.Count; moveIndex++)
            {
                Assert.NotSame(sequence.Moves[moveIndex], clone.Moves[moveIndex]);
            }
            clone.Moves[0].From = 99;
            clone.UsedDices[0] = 6;
            Assert.Equal(0, sequence.Moves[0].From);
            Assert.Equal(2, sequence.UsedDices[0]);
            sequence.Moves[1].To = 98;
            sequence.UsedDices[1] = 5;
            Assert.Equal(5, clone.Moves[1].To);
            Assert.Equal(3, clone.UsedDices[1]);
            clone.Moves.Clear();
            clone.UsedDices.Clear();
            Assert.Equal(2, sequence.Moves.Count);
            Assert.Equal(2, sequence.UsedDices.Count);
        }

        [Fact]
        public void MoveSequenceDeepCloneOfEmptySequenceIsIndependent()
        {
            var sequence = new MoveSequenceModel();

            var clone = sequence.DeepClone();

            Assert.Empty(clone.Moves);
            Assert.Empty(clone.UsedDices);
            clone.Moves.Add(M(0, 2));
            clone.UsedDices.Add(2);
            Assert.Empty(sequence.Moves);
            Assert.Empty(sequence.UsedDices);
        }
    }
}
