// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Prompter.Discord;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_MaintainerForwards.when_forwarding_a_documentation_gap;

public class and_downstream_posts_stall : given.a_forward
{
    readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly ConcurrentBag<CancellationToken> _tokens = [];
    int _posts;
    int _rejectedCallbacks;
    bool _released;

    async Task Because()
    {
        Options.Protection.ActionClicksPerUser = 10;
        var config = Microsoft.Extensions.Options.Options.Create(Options);
        var clock = new ManualClock(Clock.GetUtcNow());
        var admission = new Protection.Admission(config, new RateLimiter(config), clock);
        var actions = new NonpaidActions(admission, config, clock);
        using var rest = Discord.Client();
        var forwards = new MaintainerForwards(rest, Channel, admission, config, clock, NullLogger<MaintainerForwards>.Instance);
        Channel.Post(Arg.Any<ulong>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            var token = call.Arg<CancellationToken>();
            _tokens.Add(token);
            if (Interlocked.Increment(ref _posts) == 2) _started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, clock, token);
        });
        Task Start(ForwardClick click) => actions.Run(click.Actor, click.Channel, new(_ => Task.CompletedTask, token => forwards.Forward(click, token), (_, _) => Task.CompletedTask, (_, _) => Task.CompletedTask), TestContext.Current.CancellationToken);
        var first = Start(Documented);
        var second = Start(Requested);
        await _started.Task.WaitAsync(TestContext.Current.CancellationToken);
        await actions.Run(new(1, 2, 6), Documented.Channel, new(Rejected, RejectedWork, RejectedReply, RejectedReply), TestContext.Current.CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(5));
        await Task.WhenAll(first, second);
        await actions.Run(new(1, 2, 6), Documented.Channel, new(Released, _ => Task.FromResult("done"), (_, _) => Task.CompletedTask, (_, _) => Task.CompletedTask), TestContext.Current.CancellationToken);
    }

    Task Rejected(CancellationToken token)
    {
        _rejectedCallbacks++;
        return Task.CompletedTask;
    }

    Task<string> RejectedWork(CancellationToken token)
    {
        _rejectedCallbacks++;
        return Task.FromResult("unexpected");
    }

    Task RejectedReply(string reply, CancellationToken token) => Rejected(token);

    Task Released(CancellationToken token)
    {
        _released = true;
        return Task.CompletedTask;
    }

    [Fact] void should_hold_running_capacity_during_downstream_calls() => _rejectedCallbacks.ShouldEqual(0);
    [Fact] void should_cancel_both_downstream_posts() => _tokens.All(token => token.IsCancellationRequested).ShouldBeTrue();
    [Fact] void should_release_capacity_on_downstream_cancellation() => _released.ShouldBeTrue();
}
