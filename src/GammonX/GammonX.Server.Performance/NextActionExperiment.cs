using GammonX.Engine.Models;
using GammonX.Models.Enums;
using GammonX.Server.Contracts;
using GammonX.Server.Models;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace GammonX.Server.Performance;

internal static class NextActionExperiment
{
    private readonly record struct NextAction(int From, int To);

    // The client submits one from/to command. A contiguous prefix can move the
    // same checker across several dice before the server returns a fresh board.
    private static HashSet<NextAction> Actions(IEnumerable<MoveSequenceModel> sequences)
    {
        var actions = new HashSet<NextAction>();
        foreach (var sequence in sequences)
        {
            if (sequence.Moves.Count == 0)
            {
                continue;
            }
            var first = sequence.Moves[0];
            actions.Add(new(first.From, first.To));
            var previous = first.To;
            for (var i = 1; i < sequence.Moves.Count; i++)
            {
                var move = sequence.Moves[i];
                if (move.From != previous)
                {
                    break;
                }
                actions.Add(new(first.From, move.To));
                previous = move.To;
            }
        }
        return actions;
    }

    internal static InvocationMessage Message(EventGameStatePayload state) => Message(state, Actions(state.MoveSequences));

    internal static InvocationMessage DirectMessage(IGameSessionModel game)
    {
        var state = EventGameStatePayload.ForPlayer(game, RunSimulationModel.Player, false, true, []);
        return RunSimulationModel.Message(state);
    }

    private static InvocationMessage Message(EventGameStatePayload state, HashSet<NextAction> actions)
    {
        var payload = new
        {
            state.Modus, state.MatchId, state.Id, state.Phase, state.ActiveTurn, state.TurnNumber,
            state.DiceRolls, state.BoardState, state.AllowedCommands,
            NextActions = actions
        };
        return new InvocationMessage(ServerEventTypes.GameStateEvent, [new { Type = ServerEventTypes.GameStateEvent, Payload = payload }]);
    }

    internal static void Verify(IGameSessionModel game, bool white)
    {
        var copy = (IGameSessionModel)((ICloneable)game).Clone();
        var moves = CheckPosition(copy, white);
        if (moves.Count > 0 && moves[0].Moves.Count > 0)
        {
            var first = moves[0].Moves[0];
            copy.MoveCheckers(copy.ActivePlayer, first.From, first.To, white);
            CheckPosition(copy, white);
            copy.UndoLastMove(copy.ActivePlayer, white);
            CheckPosition(copy, white);
        }
    }

    internal static void VerifySpecialPositions(RunSimulationModel fixture)
    {
        var game = fixture.CreateGame(false);
        var fields = game.BoardModel.Fields;
        Array.Clear(fields);
        fields[22] = -15;
        fields[1] = 15;
        game.SetDiceRolls([1, 2], true);
        Verify(game, true);
        Verify(game, false);

        // White can enter with either die, but cannot spend both because point 3 is blocked.
        Array.Clear(fields);
        ((IHomeBarModel)game.BoardModel).AddToHomeBar(true, 1);
        fields[23] = -14;
        fields[2] = 2;
        fields[20] = 13;
        game.SetDiceRolls([1, 2], true);
        Verify(game, true);
    }

    private static MoveSequences CheckPosition(IGameSessionModel game, bool white)
    {
        var sequences = game.GetLegalMoveSequences(white);
        var projected = Actions(sequences);
        if (!projected.SetEquals(ResolveActions(game, white)))
        {
            throw new InvalidOperationException($"Next-action mismatch for {game.Modus}, white={white}.");
        }
        return sequences;
    }

    private static HashSet<NextAction> ResolveActions(IGameSessionModel game, bool white) =>
        game.ResolveMoveSequences(white).Select(sequence => sequence.Moves.Single())
            .Select(move => new NextAction(move.From, move.To)).ToHashSet();
}
