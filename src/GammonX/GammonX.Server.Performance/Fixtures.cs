using System.Buffers;

using GammonX.Engine.Models;
using GammonX.Engine.Services;
using GammonX.Models.Enums;
using GammonX.Models.Helpers;
using GammonX.Server;
using GammonX.Server.Contracts;
using GammonX.Diagnostics.Server;
using GammonX.Server.Models;
using GammonX.Server.Services;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Options;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Performance;

internal sealed record RunSimulationModel(string Name, GameModus Variant, int Die1, int Die2, int HistoryEvents = 0, bool Spread = false)
{
    internal static readonly Guid Player = Guid.Parse("11111111-1111-1111-1111-111111111111");
    internal static readonly Guid Opponent = Guid.Parse("22222222-2222-2222-2222-222222222222");
    internal static readonly Guid Match = Guid.Parse("33333333-3333-3333-3333-333333333333");

    internal int[] Rolls => Die1 == Die2 ? [Die1, Die1, Die1, Die1] : [Die1, Die2];

    internal IGameSessionModel CreateGame(bool rolled)
    {
        var game = new GameSessionFactory(new FixedDiceFactory(Die1, Die2)).Create(Match, Variant);
        game.StartGame(Player, Opponent);
        Configure(game);
        if (rolled)
        {
            game.SetDiceRolls([Die1, Die2], true);
        }
        return game;
    }

    internal void Configure(IGameSessionModel game)
    {
        if (Spread)
        {
            Array.Clear(game.BoardModel.Fields);
            for (var i = 2; i <= 16; i++)
            {
                game.BoardModel.Fields[i] = -1;
            }
            game.BoardModel.Fields[23] = 15;
        }

        var service = BoardServiceFactory.Create(Variant);
        for (var i = 0; i < HistoryEvents; i++)
        {
            service.AddRollEventToHistory(game.BoardModel, true, [1, 2]);
        }
    }

    internal static MetricsJsonHubProtocol CreateProtocol()
    {
        var options = new JsonHubProtocolOptions();
        options.PayloadSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
        return new MetricsJsonHubProtocol(Options.Create(options));
    }

    internal static InvocationMessage Message(EventGameStatePayload payload) =>
        new(ServerEventTypes.GameStateEvent, [new EventResponseContract<EventGameStatePayload>(ServerEventTypes.GameStateEvent, payload)]);

    // The complete repository command path, including per-command leases/commits and
    // actual SignalR serialization to both recipients. Hosting, timers and transport are excluded.
    internal Action PrepareTurn(bool nextActions = false)
    {
        var repository = CreateRepository(rolled: false);
        var protocol = CreateProtocol();
        var output = new ArrayBufferWriter<byte>();

        MoveModel? nextMove = null;
        return () =>
        {
            using (var command = new TurnCommand(repository))
            {
                command.Match.RollDices(Player);
                Publish(command);
            }

            var finished = false;
            for (var step = 0; step < 4 && !finished; step++)
            {
                using var command = new TurnCommand(repository);
                if (command.Match.CanEndTurn(Player))
                {
                    break;
                }
                var move = nextMove ?? throw new InvalidOperationException("Missing move in active payload.");
                finished = command.Match.MoveCheckers(Player, move.From, move.To);
                Publish(command);
            }

            using (var command = new TurnCommand(repository))
            {
                if (!finished)
                {
                    if (!command.Match.CanEndTurn(Player))
                    {
                        throw new InvalidOperationException("Fixture turn failed to consume its dice.");
                    }
                    command.Match.EndTurn(Player);
                    Publish(command);
                }
                command.Lease.TryRemove();
            }

            void Publish(TurnCommand command)
            {
                command.Lease.Commit();
                foreach (var player in new[] { Player, Opponent })
                {
                    output.Clear();
                    var payload = command.Match.GetGameState(player);
                    if (player == Player)
                    {
                        nextMove = payload.MoveSequences.FirstOrDefault()?.Moves[0];
                    }
                    protocol.WriteMessage(nextActions ? NextActionExperiment.Message(payload) : Message(payload), output);
                }
            }
        };
    }

    /// <summary>
    /// Prepares a repeatable game-state read through the repository lease.
    /// </summary>
    /// <param name="player">Seat whose response is measured.</param>
    /// <returns>The query to execute for each sample.</returns>
    internal Action PrepareQuery(Guid player)
    {
        var repository = CreateRepository(rolled: true);
        return () =>
        {
            var lease = Acquire(repository);
            try
            {
                GC.KeepAlive(lease.ReadGameState(player));
            }
            finally
            {
                lease.DisposeAsync().GetAwaiter().GetResult();
            }
        };
    }

    /// <summary>
    /// Prepares a bot turn with observer publication after the roll, each move, and handoff.
    /// </summary>
    /// <param name="selected">Preselected sequence used for a complete bot batch.</param>
    /// <param name="eager">Whether to generate legal sequences at each playable position.</param>
    /// <param name="fallback">Whether to select a local continuation after rolling.</param>
    /// <returns>The prepared turn to execute once.</returns>
    internal Action PrepareBotTurn(MoveSequenceModel selected, bool eager, bool fallback)
    {
        var game = CreateGame(false);
        var protocol = CreateProtocol();
        var output = new ArrayBufferWriter<byte>();
        return () =>
        {
            game.SetDiceRolls([Die1, Die2], true);
            Publish();
            var sequence = fallback ? game.GetLegalMoveSequences(true).First() : selected;
            foreach (var move in sequence.Moves)
            {
                game.MoveCheckers(Player, move.From, move.To, true);
                Publish();
            }
            if (game.CanMove(true))
            {
                throw new InvalidOperationException("Recorded bot sequence left a playable die.");
            }
            game.NextTurn(Opponent);
            Publish();
        };

        void Publish()
        {
            if (eager && game.Phase is GamePhase.Rolling or GamePhase.Moving)
            {
                GC.KeepAlive(game.GetLegalMoveSequences(true));
            }
            GC.KeepAlive(((ICloneable)game).Clone());
            output.Clear();
            protocol.WriteMessage(Message(game.ToPayload(Opponent, [], true)), output);
        }
    }

    /// <summary>
    /// Creates a committed match at the benchmark fixture position.
    /// </summary>
    /// <param name="rolled">Whether to commit the fixture dice before sampling.</param>
    /// <returns>The repository containing the prepared match.</returns>
    private MatchSessionRepository CreateRepository(bool rolled)
    {
        var repository = new MatchSessionRepository(new MatchSessionFactory(new GameSessionFactory(new FixedDiceFactory(Die1, Die2))));
        var lease = Acquire(repository);
        try
        {
            var match = lease.Create(new QueueKey(MatchVariant.Backgammon, MatchModus.Normal, MatchType.FivePointGame));
            match.JoinSession(new PlayerConnection(Player, "one"));
            match.JoinSession(new PlayerConnection(Opponent, "two"));
            match.Player1.AcceptNextGame();
            match.Player2.AcceptNextGame();
            Configure(match.StartMatch(Player));
            if (rolled)
            {
                match.RollDices(Player);
            }
            lease.Commit();
        }
        finally
        {
            lease.DisposeAsync().GetAwaiter().GetResult();
        }
        return repository;
    }

    private static MatchSessionRepository.MatchSessionLease Acquire(MatchSessionRepository repository)
    {
        var task = repository.AcquireStateAsync(Match);
        if (!task.IsCompletedSuccessfully)
        {
            throw new InvalidOperationException("Benchmark expects uncontended synchronous lease acquisition.");
        }
        return task.Result;
    }

    private sealed class TurnCommand : IDisposable
    {
        internal MatchSessionRepository.MatchSessionLease Lease { get; }
        internal IMatchSessionModel Match { get; }

        internal TurnCommand(MatchSessionRepository repository)
        {
            Lease = Acquire(repository);
            Match = Lease.Get()!;
        }

        public void Dispose() => Lease.DisposeAsync().GetAwaiter().GetResult();
    }

    private sealed class FixedDiceFactory(int first, int second) : IDiceServiceFactory
    {
        public IDiceService Create(DiceServiceType type) => new FixedDice(first, second);
    }

    private sealed class FixedDice(int first, int second) : IDiceService
    {
        public int[] Roll(int numberOfDice, int sidesPerDie)
        {
            if (numberOfDice != 2 || sidesPerDie != 6)
            {
                throw new InvalidOperationException("Benchmark only rolls two six-sided dice.");
            }
            return [first, second];
        }
    }
}
