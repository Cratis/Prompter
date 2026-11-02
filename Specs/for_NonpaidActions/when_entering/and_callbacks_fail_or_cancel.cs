// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_NonpaidActions.when_entering;

public class and_callbacks_fail_or_cancel : given.a_nonpaid_action
{
    bool _faultPropagated;
    bool _cancelled;
    bool _capacityReleased;

    void Establish()
    {
        Options.Protection.ActionConcurrency = 1;
        Options.Protection.ActionClicksPerUser = 10;
        Create();
    }

    async Task Because()
    {
        Work = _ => throw new Failed();
        try
        {
            await Run();
        }
        catch (Failed)
        {
            _faultPropagated = true;
        }
        using var caller = new CancellationTokenSource();
        CancellationToken received = default;
        Work = async token =>
        {
            received = token;
            await Task.Delay(Timeout.InfiniteTimeSpan, Clock, token);
            return "unexpected";
        };
        var running = Run(cancellationToken: caller.Token);
        await caller.CancelAsync();
        await running;
        _cancelled = received.IsCancellationRequested;
        Events.Clear();
        Work = _ =>
        {
            _capacityReleased = true;
            return Task.FromResult("done");
        };
        await Run();
    }

    [Fact] void should_preserve_callback_failures() => _faultPropagated.ShouldBeTrue();
    [Fact] void should_pass_caller_cancellation_to_downstream_work() => _cancelled.ShouldBeTrue();
    [Fact] void should_release_capacity_after_failures_and_cancellation() => _capacityReleased.ShouldBeTrue();
    [Fact] void should_deliver_the_next_action_after_releasing_capacity() => Events.ToArray().ShouldEqual(["defer", "followup"]);

    sealed class Failed : Exception;
}
