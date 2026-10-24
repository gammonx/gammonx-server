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

        [Fact]
        public void MoveSequencesConstructorCopiesCollectionAndPreservesMoveSelection()
        {
            var firstSequence = Seq(M(18, 13), M(13, 8));
            var secondSequence = Seq(M(18, 12), M(12, 6));
            var source = new List<MoveSequenceModel> { firstSequence, secondSequence };

            var sequences = new MoveSequences(source.Select(sequence => sequence));

            source.Clear();
            Assert.Equal(2, sequences.Count);
            Assert.Same(firstSequence, sequences[0]);
            Assert.Same(secondSequence, sequences[1]);
            Assert.True(sequences.CanMove);
            Assert.True(sequences.TryUseMove(18, 8, out var played));
            Assert.Equal(firstSequence.Moves, played);
        }

        [Fact]
        public void MoveSequencesConstructorAcceptsEmptyCollection()
        {
            var sequences = new MoveSequences(Array.Empty<MoveSequenceModel>());

            Assert.Empty(sequences);
            Assert.False(sequences.CanMove);
            Assert.False(sequences.TryUseMove(0, 2, out var played));
            Assert.Empty(played);
        }

        [Fact]
        public void SingleMoveIsPreferredOverCombinedMove()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 23),     // die 5
                    M(23, -100)    // die 6
                ),
                Seq(
                    M(18, -100)   // die 11 (bear-off)
                )
            };

            var result = sequences.TryUseMove(18, -100, out var played);

            Assert.True(result);
            Assert.Single(played);
            Assert.Equal(M(18, -100), played[0]);
        }

        [Fact]
        public void CombinedMoveLength2IsResolvedCorrectly()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 13),
                    M(13, 8)
                )
            };

            var result = sequences.TryUseMove(18, 8, out var played);

            Assert.True(result);
            Assert.Equal(2, played.Count);
            Assert.Equal(M(18, 13), played[0]);
            Assert.Equal(M(13, 8), played[1]);
        }

        [Fact]
        public void PaschPrefixOfTwoMovesIsValid()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 13),
                    M(13, 8),
                    M(8, 3),
                    M(3, -100)
                )
            };

            var result = sequences.TryUseMove(18, 8, out var played);

            Assert.True(result);
            Assert.Equal(2, played.Count);
        }

        [Fact]
        public void PaschPrefixOfThreeMovesIsValid()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 13),
                    M(13, 8),
                    M(8, 3),
                    M(3, -100)
                )
            };

            var result = sequences.TryUseMove(18, 3, out var played);

            Assert.True(result);
            Assert.Equal(3, played.Count);
        }

        [Fact]
        public void PaschFullPrefixIsValid()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 13),
                    M(13, 8),
                    M(8, 3),
                    M(3, -100)
                )
            };

            var result = sequences.TryUseMove(18, -100, out var played);

            Assert.True(result);
            Assert.Equal(4, played.Count);
        }

        [Fact]
        public void NonContiguousChainIsRejected()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 13),
                    M(12, 8),
                    M(8, 3)
                )
            };

            var result = sequences.TryUseMove(18, 3, out var played);

            Assert.False(result);
            Assert.Empty(played);
        }

        [Fact]
        public void SuffixChainIsRejected()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 13),
                    M(13, 8),
                    M(8, 3)
                )
            };

            var result = sequences.TryUseMove(13, 3, out var played);

            Assert.False(result);
            Assert.Empty(played);
        }

        [Fact]
        public void MatchingLaterToIndexDoesNotOverridePrefixRule()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 13),
                    M(13, 8),
                    M(8, 13) // same TO as first move
                )
            };

            var result = sequences.TryUseMove(18, 13, out var played);

            Assert.True(result);
            Assert.Single(played);
            Assert.Equal(M(18, 13), played[0]);
        }

        [Fact]
        public void BearOffAmbiguityExactMatchWins()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 23),
                    M(23, -100)
                ),
                Seq(
                    M(18, -100)
                )
            };

            var result = sequences.TryUseMove(18, -100, out var played);

            Assert.True(result);
            Assert.Single(played);
            Assert.Equal(M(18, -100), played[0]);
        }

        [Fact]
        public void CombinedMoveMustStartAtFirstMove()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 13),
                    M(13, 8)
                )
            };

            // illegal combined move, skipping the first move
            var result = sequences.TryUseMove(13, 3, out var played);

            Assert.False(result);
            Assert.Empty(played);
        }

        [Fact]
        public void CorrectSequenceIsSelectedWhenMultipleExist()
        {
            var sequences = new MoveSequences
            {
                Seq(
                    M(18, 13),
                    M(13, 8)
                ),
                Seq(
                    M(18, 12),
                    M(12, 6)
                )
            };

            var result = sequences.TryUseMove(18, 8, out var played);

            Assert.True(result);
            Assert.Equal(2, played.Count);
            Assert.Equal(M(18, 13), played[0]);
        }
    }
}
