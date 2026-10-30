// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.Fakes;

/// <summary>Captures a request deadline and fires it explicitly without sleeping.</summary>
public sealed class DeadlineClock : TimeProvider
{
    TimerCallback _callback = null!;
    object? _state;

    /// <summary>Gets the scheduled deadline.</summary>
    public TimeSpan DueTime { get; private set; }

    /// <inheritdoc/>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        _callback = callback;
        _state = state;
        DueTime = dueTime;
        return Substitute.For<ITimer>();
    }

    /// <summary>Fires the captured deadline synchronously.</summary>
    public void Expire() => _callback(_state);
}
