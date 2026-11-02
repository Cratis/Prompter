// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.Fakes;

/// <summary>A time provider whose periodic timers fire only when a spec calls <see cref="Tick"/>.</summary>
public sealed class ManualTimeProvider : TimeProvider
{
    readonly List<ManualTimer> _timers = [];

    /// <summary>Gets the number of timers created so far.</summary>
    public int TimerCount => _timers.Count;

    /// <summary>Fires every live timer once, as if its interval had elapsed.</summary>
    public void Tick()
    {
        foreach (var timer in _timers.ToArray())
        {
            timer.Fire();
        }
    }

    /// <inheritdoc/>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(callback, state);
        _timers.Add(timer);
        return timer;
    }

    sealed class ManualTimer(TimerCallback callback, object? state) : ITimer
    {
        bool _disposed;

        public void Fire()
        {
            if (!_disposed)
            {
                callback(state);
            }
        }

        public bool Change(TimeSpan dueTime, TimeSpan period) => !_disposed;

        public void Dispose() => _disposed = true;

        public ValueTask DisposeAsync()
        {
            _disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
