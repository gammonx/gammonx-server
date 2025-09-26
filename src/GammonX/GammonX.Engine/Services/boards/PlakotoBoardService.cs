using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    // <inheritdoc />
    internal class PlakotoBoardService : BoardBaseServiceImpl
    {
        // <inheritdoc />
        public override GameModus Modus => GameModus.Plakoto;

        // <inheritdoc />
        public override IBoardModel CreateBoard()
        {
            return new PlakotoBoardModelImpl();
        }

        // <inheritdoc />
        public override void MoveCheckerTo(IBoardModel model, int from, int to, bool isWhite)
        {
            var pinModel = model as IPinModel;
            // we do not have to check for pinned checker if the move bears the moving checker off
            if (!IsBearOffMove(model, to ,isWhite))
            {
				if (pinModel != null)
				{
					// we check if we would pin an opponents checker with this move
					EvaluatePinnedCheckers(model, from, to, isWhite);
				}
			}

            base.MoveCheckerTo(model, from, to, isWhite);

            if (pinModel != null)
            {
                // we check if we would unpin an opponents checker with this move
                EvaluateUnPinnedCheckers(model, from, isWhite);
            }
        }

		// <inheritdoc />
		public override bool CanBearOffChecker(IBoardModel model, int from, int roll, bool isWhite)
		{
            if (model is IPinModel pinModel)
            {
                if (isWhite)
                {
                    var pinnedWhiteCheckers = pinModel.PinnedFields.FirstOrDefault(pf => pf < 0);
                    if (pinnedWhiteCheckers != default(int))
                    {
                        return false;
                    }
                }
                else
                {
					var pinnedBlackCheckers = pinModel.PinnedFields.FirstOrDefault(pf => pf > 0);
					if (pinnedBlackCheckers != default(int))
					{
						return false;
					}
				}
            }

            return base.CanBearOffChecker(model, from, roll, isWhite);
		}

        private static void EvaluatePinnedCheckers(IBoardModel model, int from, int to, bool isWhite)
        {
            // we know that the checker can be pinned, otherwise the move could not have been made
            if (isWhite)
            {
                // we detect a black checker on the target field
                if (model.Fields[to] > 0)
                {
                    PinChecker(model, to, isWhite);
                }
            }
            else
            {
                // we detect a white checker on the target field
                if (model.Fields[to] < 0)
                {
					PinChecker(model, to, isWhite);
                }
            }
        }

        private static void EvaluateUnPinnedCheckers(IBoardModel model, int from, bool isWhite)
        {
            // we check if we released a pinned checker
            if (model.Fields[from] == 0)
            {
                UnPinChecker(model, from, isWhite);
            }
        }

        private static void PinChecker(IBoardModel model, int fieldIndex, bool isWhite)
        {
            if (model is IPinModel pinModel)
            {
                if (isWhite)
                {
                    // we move the black checker to the pinned fields list
                    pinModel.PinnedFields.SetValue(pinModel.PinnedFields[fieldIndex] += 1, fieldIndex);
                    // we removed it from the moveable fields array
                    model.Fields.SetValue(model.Fields[fieldIndex] -= 1, fieldIndex);
                }
                else
                {
                    // we move the white checker to the pinned fields list
                    pinModel.PinnedFields.SetValue(pinModel.PinnedFields[fieldIndex] -= 1, fieldIndex);
                    // we removed it from the moveable fields array
                    model.Fields.SetValue(model.Fields[fieldIndex] += 1, fieldIndex);
                }
            }
            else
            {
                throw new InvalidOperationException("Model does not support pinning checkers.");
            }
        }

        private static void UnPinChecker(IBoardModel model, int fieldIndex, bool isWhite)
        {
            if (model is IPinModel pinModel)
            {
                // we check if there is a pinned checker on the field
                if (pinModel.PinnedFields[fieldIndex] == 0)
                    return;

                if (isWhite)
                {
                    // we remove the black checker to the pinned fields list
                    pinModel.PinnedFields.SetValue(pinModel.PinnedFields[fieldIndex] -= 1, fieldIndex);
                    // we removed it from the moveable fields array
                    model.Fields.SetValue(model.Fields[fieldIndex] += 1, fieldIndex);
                }
                else
                {
                    // we remove the white checker to the pinned fields list
                    pinModel.PinnedFields.SetValue(pinModel.PinnedFields[fieldIndex] += 1, fieldIndex);
                    // we move it from the moveable fields array
                    model.Fields.SetValue(model.Fields[fieldIndex] -= 1, fieldIndex);
                }
            }
            else
            {
                throw new InvalidOperationException("Model does not support pinning checkers.");
            }
        }
    }
}