// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Operations;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Prompter.Specs.for_WebhookProcessing.when_starting;

public class and_distinct_events_are_blocked : Specification
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _created;
    int _accepted;
    int _peak;
    int _active;

    async Task Because()
    {
        var options = new PrompterOptions();
        options.Protection.WebhookPerMinute = 2;
        var clock = new ManualClock(new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var processing = new WebhookProcessing(Microsoft.Extensions.Options.Options.Create(options), clock, NullLogger<WebhookProcessing>.Instance);
        for (var index = 0; index < 50; index++)
        {
            if (processing.TryStart(Work, default)) _accepted++;
        }
        await _started.Task.WaitAsync(TimeSpan.FromSeconds(5), clock);
        _release.SetResult();
        await _finished.Task.WaitAsync(TimeSpan.FromSeconds(5), clock);
        processing.TryStart(Work, default).ShouldBeFalse();
    }

    async Task Work(CancellationToken token)
    {
        Interlocked.Increment(ref _created);
        var active = Interlocked.Increment(ref _active);
        Interlocked.Exchange(ref _peak, Math.Max(_peak, active));
        if (_created == 2) _started.TrySetResult();
        await _release.Task.WaitAsync(token);
        if (Interlocked.Decrement(ref _active) == 0) _finished.TrySetResult();
    }

    [Fact] void should_accept_only_two_events() => _accepted.ShouldEqual(2);
    [Fact] void should_not_create_work_for_rejected_events() => _created.ShouldEqual(2);
    [Fact] void should_bound_concurrent_payloads() => _peak.ShouldEqual(2);
}
