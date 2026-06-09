using GammonX.Engine.Models;

using GammonX.Mars.NN.Models;

using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Tests.Models
{
    public class FinalEvalResultModelsTests
    {
        private readonly FinalEvalResultModels _models = new FinalEvalResultModels(new FinalEvalResultModel[]
        {
            new FinalEvalResultModel(10, new MoveSequenceModel() {Moves = new List<MoveModel> {new MoveModel(1, 2), new MoveModel(1, 2)}}, new NormalizedEvalResultModel()),
            new FinalEvalResultModel(11, new MoveSequenceModel() {Moves = new List<MoveModel> {new MoveModel(2, 3), new MoveModel(2, 3)}}, new NormalizedEvalResultModel()),
            new FinalEvalResultModel(15, new MoveSequenceModel() {Moves = new List<MoveModel> {new MoveModel(3, 4), new MoveModel(3, 4)}}, new NormalizedEvalResultModel()),
            new FinalEvalResultModel(20, new MoveSequenceModel() {Moves = new List<MoveModel> {new MoveModel(4, 5), new MoveModel(4, 5)}}, new NormalizedEvalResultModel()),
            new FinalEvalResultModel(30, new MoveSequenceModel() {Moves = new List<MoveModel> {new MoveModel(5, 6), new MoveModel(5, 6)}}, new NormalizedEvalResultModel())
        });

        private readonly FinalEvalResultModels _singleModel = new FinalEvalResultModels(new FinalEvalResultModel[]
        {
            new FinalEvalResultModel(10, new MoveSequenceModel() {Moves = new List<MoveModel> {new MoveModel(1, 2), new MoveModel(1, 2)}}, new NormalizedEvalResultModel()),
        });

        [Fact]
        public void BotLevelEasyReturnsRandom()
        {
            var result = _models.Select(BotLevel.Easy);
            Assert.True(result == _models[3].MoveSequence || result == _models[4].MoveSequence || result == _models[2].MoveSequence);
            Assert.NotEqual(_models[0].MoveSequence, result);
            Assert.NotEqual(_models[1].MoveSequence, result);
        }

        [Fact]
        public void BotLevelMediumReturnsSelectedRandom()
        {
            var result = _models.Select(BotLevel.Medium);
            Assert.True(result == _models[0].MoveSequence || result == _models[1].MoveSequence || result == _models[2].MoveSequence);
            Assert.NotEqual(_models[3].MoveSequence, result);
            Assert.NotEqual(_models[4].MoveSequence, result);
        }

        [Fact]
        public void BotLevelHardReturnsBest()
        {
            var result = _models.Select(BotLevel.Hard);
            Assert.Equal(_models[0].MoveSequence, result);
        }

        [Fact]
        public void BotLevelEvaluatorHandlesEdgeCases()
        {
            var emptyModels = new FinalEvalResultModels(Array.Empty<FinalEvalResultModel>());
            Assert.Throws<InvalidOperationException>(() => emptyModels.Select(BotLevel.Unknown));
            var emptyMoveSeq = emptyModels.Select(BotLevel.Easy);
            Assert.Empty(emptyMoveSeq.Moves);
            emptyMoveSeq = emptyModels.Select(BotLevel.Medium);
            Assert.Empty(emptyMoveSeq.Moves);
            emptyMoveSeq = emptyModels.Select(BotLevel.Hard);
            Assert.Empty(emptyMoveSeq.Moves);
        }

        [Fact]
        public void BotLevelsHandleSingleOutput()
        {
            var result = _singleModel.Select(BotLevel.Hard);
            Assert.Equal(_singleModel[0].MoveSequence, result);
            result = _singleModel.Select(BotLevel.Medium);
            Assert.Equal(_singleModel[0].MoveSequence, result);
            result = _singleModel.Select(BotLevel.Easy);
            Assert.Equal(_singleModel[0].MoveSequence, result);
        }
    }
}
