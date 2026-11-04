// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.Fakes;

public sealed class ManualClock(DateTimeOffset now) : TimeProvider
{
    readonly Lock _gate = new();
    readonly List<ManualTimer> _timers = [];
    DateTimeOffset _now = now;

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate) return _now;
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        lock (_gate) _timers.Add(timer);
        timer.Change(dueTime, period);
        return timer;
    }

    public void Advance(TimeSpan by)
    {
        DateTimeOffset target;
        lock (_gate) target = _now + by;

        // Callbacks run outside the lock so they can dispose or create timers.
        while (TakeNextDue(target) is { } next)
        {
            next.Fire();
        }

        lock (_gate) _now = target;
    }

    ManualTimer? TakeNextDue(DateTimeOffset target)
    {
        lock (_gate)
        {
            var next = _timers.Where(timer => timer.Due <= target).OrderBy(timer => timer.Due).FirstOrDefault();
            if (next is not null) _now = next.Due;
            return next;
        }
    }

    sealed class ManualTimer(ManualClock clock, TimerCallback callback, object? state) : ITimer
    {
        TimeSpan _period;
        bool _disposed;

        public DateTimeOffset Due { get; private set; } = DateTimeOffset.MaxValue;

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            lock (clock._gate)
            {
                if (_disposed) return false;
                _period = period;
                Due = dueTime == Timeout.InfiniteTimeSpan ? DateTimeOffset.MaxValue : clock._now + dueTime;
                return true;
            }
        }

        public void Fire()
        {
            lock (clock._gate) Due = _period > TimeSpan.Zero ? Due + _period : DateTimeOffset.MaxValue;
            callback(state);
        }

        public void Dispose() => Remove();

        public ValueTask DisposeAsync()
        {
            Remove();
            return ValueTask.CompletedTask;
        }

        void Remove()
        {
            lock (clock._gate)
            {
                _disposed = true;
                clock._timers.Remove(this);
            }
        }
    }
}
