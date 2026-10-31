// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.Fakes;

public sealed class ManualClock(DateTimeOffset now) : TimeProvider
{
    readonly List<ManualTimer> _timers = [];
    DateTimeOffset _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        _timers.Add(timer);
        timer.Change(dueTime, period);
        return timer;
    }

    public void Advance(TimeSpan by)
    {
        var target = _now + by;
        while (_timers.Where(timer => timer.Due <= target).OrderBy(timer => timer.Due).FirstOrDefault() is { } next)
        {
            _now = next.Due;
            next.Fire();
        }

        _now = target;
    }

    sealed class ManualTimer(ManualClock clock, TimerCallback callback, object? state) : ITimer
    {
        TimeSpan _period;
        bool _disposed;

        public DateTimeOffset Due { get; private set; } = DateTimeOffset.MaxValue;

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            if (_disposed) return false;
            _period = period;
            Due = dueTime == Timeout.InfiniteTimeSpan ? DateTimeOffset.MaxValue : clock._now + dueTime;
            return true;
        }

        public void Fire()
        {
            Due = _period > TimeSpan.Zero ? Due + _period : DateTimeOffset.MaxValue;
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
            _disposed = true;
            clock._timers.Remove(this);
        }
    }
}
