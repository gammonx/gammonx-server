namespace GammonX.Server.Tests.Integration;

/// <summary>
/// Provides controlled UTC time and one-shot delays. Tests explicitly release due timer callbacks.
/// </summary>
internal sealed class DeadlineTestClock : TimeProvider
{
    private readonly Lock _gate = new();
    private readonly HashSet<OneShotTimer> _timers = new();
    private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
        {
            return _now;
        }
    }

    internal void SetUtcNow(DateTimeOffset now)
    {
        lock (_gate)
        {
            _now = now;
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new OneShotTimer(this, callback, state);
        timer.Change(dueTime, period);
        return timer;
    }

    internal void FireDueTimers()
    {
        OneShotTimer[] due;
        lock (_gate)
        {
            due = _timers.Where(timer => timer.DueAt <= _now).ToArray();
            _timers.ExceptWith(due);
        }

        foreach (var timer in due)
        {
            timer.Fire();
        }
    }

    private sealed class OneShotTimer(DeadlineTestClock clock, TimerCallback callback, object? state) : ITimer
    {
        private bool _disposed;

        internal DateTimeOffset DueAt { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            Assert.Equal(Timeout.InfiniteTimeSpan, period);
            lock (clock._gate)
            {
                if (_disposed)
                {
                    return false;
                }

                clock._timers.Remove(this);
                if (dueTime != Timeout.InfiniteTimeSpan)
                {
                    DueAt = clock._now + dueTime;
                    clock._timers.Add(this);
                }

                return true;
            }
        }

        internal void Fire() => callback(state);

        public void Dispose()
        {
            lock (clock._gate)
            {
                _disposed = true;
                clock._timers.Remove(this);
            }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
