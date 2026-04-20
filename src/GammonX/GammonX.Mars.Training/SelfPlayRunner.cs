using GammonX.Engine.Extensions;
using GammonX.Engine.Services;

using GammonX.Mars.Server;
using GammonX.Mars.Server.Services;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Mars.Training
{
    public sealed class SelfPlayRunner
    {
        private readonly GameModus _modus;
        private readonly SelfPlayRecorder _recorder;

        public SelfPlayRunner(SelfPlayRecorder recorder, GameModus modus)
        {
            _recorder = recorder;
            _modus = modus;
        }

        public IReadOnlyList<(float[] Features, float Label)> Run()
        {
            var boardService = BoardServiceFactory.Create(_modus);
            var board = boardService.CreateBoard();
            var evalService = FeatureEvalServiceFactory.Create(_modus);
            var diceService = new DiceServiceFactory().Create(DiceServiceType.Simple);

            var isWhite = true;
            const int maxTurns = 250;
            var turnCount = 0;

            while (board.BearOffCountBlack != board.WinConditionCount
                && board.BearOffCountWhite != board.WinConditionCount
                && turnCount < maxTurns)
            {
                turnCount++;
                var rolls = diceService.Roll(2, 6);
                rolls = rolls[0] == rolls[1]
                    ? [rolls[0], rolls[0], rolls[0], rolls[0]]
                    : [rolls[0], rolls[1]];

                var evalRequest = new EvalMoveRequestContract()
                {
                    Board = board.ToContract(false),
                    IsWhite = isWhite,
                    Modus = _modus,
                    Rolls = rolls
                };

                // TODO: only plakoto weights supported atm
                var result = evalService.EvalMoveSequenceForTraining(
                    evalRequest, 
                    EvalWeights.PlakotoCheapContactWeights, 
                    EvalWeights.PlakotoContactWeights, 
                    EvalWeights.RaceWeights,
                    150);

                if (result.BestMove.Moves.Count != 0)
                {
                    _recorder.RecordPosition(result.EvalResult, isWhite);

                    foreach (var move in result.BestMove.Moves)
                        boardService.MoveCheckerTo(board, move.From, move.To, isWhite);
                }

                isWhite = !isWhite;
            }

            if (turnCount >= maxTurns)
                return [];

            var whiteWon = board.BearOffCountWhite == board.WinConditionCount;
            return _recorder.Finalize(whiteWon);
        }
    }
}
