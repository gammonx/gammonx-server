using GammonX.Diagnostics.Server;

using Serilog;

namespace GammonX.Server.Services;

internal readonly record struct BotTurnIdentity(
    Guid MatchId, 
    Guid BotId, 
    Guid GameId, 
    int TurnNumber);

/// <summary>
/// Owns delayed bot turn retries and drains them during host shutdown.
/// </summary>
internal sealed class BotTurnRecoveryService(
    ICancellationTokenService tokens,
    TimeProvider clock,
    IHostApplicationLifetime? lifetime = null) : IHostedService, IDisposable
{
    internal static readonly TimeSpan RecoveryDelay = TimeSpan.FromSeconds(1);

    internal sealed record Deadline(
        BotTurnIdentity Turn, 
        CancellationTokenKey Key, 
        DateTimeOffset DueAt, 
        CancellationTokenSource Generation);

    private readonly Lock _gate = new();

    private readonly HashSet<Task> _pending = new();

    private readonly CancellationTokenSource _shutdown = CancellationTokenSource.CreateLinkedTokenSource(lifetime?.ApplicationStopping ?? CancellationToken.None);
    
    private bool _stopping;

    internal CancellationToken Stopping => _shutdown.Token;

    internal void Schedule(BotTurnIdentity turn, CancellationToken disconnected, Func<Deadline, CancellationToken, Task> recover)
    {
        lock (_gate)
        {
            if (_stopping || Stopping.IsCancellationRequested || disconnected.IsCancellationRequested)
            {
                return;
            }

            var key = new CancellationTokenKey
            {
                MatchId = turn.MatchId,
                PlayerId = turn.BotId,
                Category = CancellationTokenCategory.BotRecovery
            };

            var generation = new CancellationTokenSource();
            var deadline = new Deadline(turn, key, clock.GetUtcNow() + RecoveryDelay, generation);
            tokens.Store(key, generation);
            
            if (ExecutionContext.IsFlowSuppressed())
            {
                StartRecovery();
            }
            else
            {
                using (ExecutionContext.SuppressFlow())
                {
                    StartRecovery();
                }
            }

            void StartRecovery()
            {
                var task = RunAsync(deadline, disconnected, recover);
                _pending.Add(task);
                _ = ForgetAsync(task);
            }
        }
    }

    private async Task RunAsync(Deadline deadline, CancellationToken disconnected, Func<Deadline, CancellationToken, Task> recover)
    {
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(Stopping, disconnected);
        using var generation = deadline.Generation;
        using var delay = CancellationTokenSource.CreateLinkedTokenSource(operation.Token, generation.Token);
        ServerMetrics.TimerTaskStarted();
        
        try
        {
            var remaining = deadline.DueAt - clock.GetUtcNow();
            while (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining, clock, delay.Token);
                remaining = deadline.DueAt - clock.GetUtcNow();
            }

            delay.Token.ThrowIfCancellationRequested();
            // Consuming the registration under the match lease cancels the delay, while the
            // recovery itself still observes the connection and host tokens.
            using var callback = new ServerMetrics.CallbackScope();
            await recover(deadline, operation.Token);
        }
        catch (OperationCanceledException) when (delay.IsCancellationRequested)
        {
            // pass
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Bot recovery failed for match {MatchId}, game {GameId}, turn {TurnNumber}.", deadline.Turn.MatchId, deadline.Turn.GameId, deadline.Turn.TurnNumber);
        }
        finally
        {
            tokens.TryCancel(deadline.Key, generation);
            ServerMetrics.TimerTaskFinished();
        }
    }

    private async Task ForgetAsync(Task task)
    {
        await task;

        lock (_gate)
        {
            _pending.Remove(task);
        }
    }

    // <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Task[] pending;

        lock (_gate)
        {
            _stopping = true;
            pending = _pending.ToArray();
        }

        await _shutdown.CancelAsync();
        await Task.WhenAll(pending).WaitAsync(cancellationToken);
    }

    // <inheritdoc />
    public void Dispose() => _shutdown.Dispose();
}
