using GammonX.Server.Models;

namespace GammonX.Server
{
    /// <summary>
    /// Gets a list of all commands the web socket hub can receive.
    /// </summary>
    public static class ServerCommands
    {
        /// <summary>
        /// The caller can join a match session with an existing match id created by the matchmaking service.
        /// </summary>
        public const string JoinMatchCommand = "JoinMatch";

        /// <summary>
        /// The caller started the match and rolls his dices to decided whether he or the opponent starts the game.
        /// </summary>
        public const string StartMatchCommand = "StartMatch";

        /// <summary>
        /// The caller can start a game round if successfuly joined a match session.
        /// </summary>
        public const string StartGameCommand = "StartGame";

        /// <summary>
        /// If the active player, the caller can roll the dices and start his turn.
        /// </summary>
        public const string RollCommand = "Roll";

        /// <summary>
        /// If the active player, the caller can move his checkers from one position to another.
        /// </summary>
        public const string MoveCommand = "Move";

        /// <summary>
        /// If the active player, the caller can undo hist last move.
        /// </summary>
        public const string UndoMoveCommand = "UndoMove";

        /// <summary>
        /// If the active player, the caller can end his turn and switch to the next player.
        /// </summary>
        public const string EndTurnCommand = "EndTurn";

        /// <summary>
        /// The calling player resigns the match and loses automatically.
        /// </summary>
        public const string ResignMatchCommand = "ResignMatch";

        /// <summary>
        /// The calling player resigns the game and loses as a back-/gammon.
        /// </summary>
        public const string ResignGameCommand = "ResignGame";

        /// <summary>
        /// The doubling cube owner offers a double.
        /// </summary>
        public const string OfferDoubleCommand = "OfferDouble";

        /// <summary>
        /// The non doubling cube owner accepts the double offering.
        /// </summary>
        public const string AcceptDoubleCommand = "AcceptDouble";

        /// <summary>
        /// The non doubling cube owner declines the double offering and automatically loses as a backgammon.
        /// </summary>
        public const string DeclineDoubleCommand = "DeclineDouble";

        /// <summary>
        /// Gets a list of allowed commands that can follow up the given <paramref name="previousCommand"/>.
        /// </summary>
        /// <param name="match">Match to base on.</param>
        /// <param name="callingPlayerId">Id of the calling player.</param>
        /// <param name="previousCommand">Previously executed command.</param>
        /// <returns>A list of allowed commands.</returns>
        public static string[] GetAllowedCommands(IMatchSessionModel match, Guid callingPlayerId, string previousCommand)
        {
            var game = match.GetGameSession(match.GameRound);
            var activePlayer = game?.ActivePlayer == callingPlayerId;

            // game has not yet started
            if (game == null)
            {
                var player1 = match.Player1;
                var player2 = match.Player2;

                // bot players joined the match
                if (player1.Id == callingPlayerId && player1.Claimed)
                {
                    // player 1 has started, player 2 not yet
                    if (player1.Id == callingPlayerId && player1.NextGameAccepted && !player2.NextGameAccepted)
                    {
                        return new string[] { ResignGameCommand, ResignMatchCommand };
                    }
                    // player 1 has to start the match and the first game with his initial roll or force reroll if equals
                    else if (previousCommand.Equals(JoinMatchCommand) || player1.StartDiceRoll == player2.StartDiceRoll)
                    {
                        return new string[] { StartMatchCommand, ResignGameCommand, ResignMatchCommand };
                    }
                    else
                    {
                        return new string[] { ResignGameCommand, ResignMatchCommand };
                    }
                }
                else if (!player1.Claimed)
                {
                    // waiting for all players to join the match
                    return new string[] { JoinMatchCommand, ResignGameCommand, ResignMatchCommand };
                }

                if (player2.Id == callingPlayerId && player2.Claimed)
                {
                    // player 2 has started, player 1 not yet
                    if (player2.Id == callingPlayerId && player2.NextGameAccepted && !player1.NextGameAccepted)
                    {
                        return new string[] { ResignGameCommand, ResignMatchCommand };
                    }
                    // player 2 has to start the match and the first game with his initial roll or forcereroll if equals
                    else if (previousCommand.Equals(JoinMatchCommand) || player1.StartDiceRoll == player2.StartDiceRoll)
                    {
                        return new string[] { StartMatchCommand, ResignGameCommand, ResignMatchCommand };
                    }
                    else
                    {
                        return new string[] { ResignGameCommand, ResignMatchCommand };
                    }
                }
                else if (!player2.Claimed)
                {
                    // waiting for all players to join the match
                    return new string[] { JoinMatchCommand, ResignGameCommand, ResignMatchCommand };
                }
            }

            // we check if the active game session is over
            if (game != null && game.Phase == GamePhase.GameOver)
            {
                if (match.IsMatchOver())
                {
                    return Array.Empty<string>();
                }
                else
                {
                    // both players have to start the next game if one is available
                    return new string[] { StartGameCommand, ResignGameCommand, ResignMatchCommand };
                }
            }

            if (activePlayer && game != null)
            {
                switch (previousCommand)
                {
                    case JoinMatchCommand:
                        return new string[] { StartGameCommand, ResignGameCommand, ResignMatchCommand };
                    case StartMatchCommand:
                        // the first game of the match is imiplicitly started by their initial roll
                        // the palyer who won the initial roll starts the game with the given dices
                        // and can directly move his checkers
                        // subsequent games have to be started with 'StartGameCommand' and the starting player
                        // is implicitly defined by the winner of the previous game.
                        if (game.DiceRolls.Any())
                            return new string[] { MoveCommand, ResignGameCommand, ResignMatchCommand };
                        else
                            return new string[] { RollCommand, ResignGameCommand, ResignMatchCommand };
                    case StartGameCommand:
                        return new string[] { RollCommand, ResignGameCommand, ResignMatchCommand };
                    case RollCommand:
                        {
                            if (match.CanEndTurn(callingPlayerId))
                                return new string[] { EndTurnCommand, ResignGameCommand, ResignMatchCommand };
                            else
                                return new string[] { MoveCommand, ResignGameCommand, ResignMatchCommand };
                        }
                    case MoveCommand:
                        {
                            if (game.Phase == GamePhase.GameOver)
                            {
                                if (match.IsMatchOver())
                                {
                                    return Array.Empty<string>();
                                }
                                else
                                {
                                    return new string[] { StartGameCommand, ResignGameCommand, ResignMatchCommand };
                                }
                            }
                            else if (match.CanEndTurn(callingPlayerId))
                            {
                                return new string[] { EndTurnCommand, UndoMoveCommand, ResignGameCommand, ResignMatchCommand };
                            }
                            else
                            {
                                return new string[] { MoveCommand, UndoMoveCommand, ResignGameCommand, ResignMatchCommand };
                            }
                        }
                    case UndoMoveCommand:
                        if (match.CanUndoLastMove(callingPlayerId))
                        {
                            return new string[] { MoveCommand, UndoMoveCommand, ResignGameCommand, ResignMatchCommand };
                        }
                        else
                        {
                            return new string[] { MoveCommand, ResignGameCommand, ResignMatchCommand };
                        }
                    case EndTurnCommand:
                        return new string[] { RollCommand, ResignGameCommand, ResignMatchCommand };
                    case ResignMatchCommand:
                        return Array.Empty<string>();
                    case ResignGameCommand:
                        return new string[] { StartGameCommand, ResignGameCommand, ResignMatchCommand };
                    case OfferDoubleCommand:
                        return new string[] { AcceptDoubleCommand, DeclineDoubleCommand, ResignGameCommand, ResignMatchCommand };
                    case AcceptDoubleCommand:
                        return new string[] { RollCommand, ResignGameCommand, ResignMatchCommand };
                    case DeclineDoubleCommand:
                        return new string[] { StartGameCommand, ResignGameCommand, ResignMatchCommand };
                    default:
                        throw new ArgumentException($"The given command is not known", nameof(previousCommand));
                }
            }
            else
            {
                return new string[] { ResignGameCommand, ResignMatchCommand };
            }
        }
    }
}
