// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_NonpaidActions.when_entering;

public class and_metadata_is_not_trusted : given.a_nonpaid_action
{
    int _unknownChannelCalls;

    void Establish() => Create();

    async Task Because()
    {
        await Run(Actor with { UserId = 0 });
        await Run(Actor with { GuildId = null });
        await Run(Actor with { ChannelId = 99 });
        var callbacks = new NonpaidCallbacks(Observe, ObserveWork, ObserveReply, ObserveReply);
        await Actions.Run(Actor, null, callbacks, TestContext.Current.CancellationToken);
    }

    Task Observe(CancellationToken token)
    {
        _unknownChannelCalls++;
        return Task.CompletedTask;
    }

    Task<string> ObserveWork(CancellationToken token)
    {
        _unknownChannelCalls++;
        return Task.FromResult("unexpected");
    }

    Task ObserveReply(string reply, CancellationToken token) => Observe(token);

    [Fact] void should_not_acknowledge_or_lookup_untrusted_metadata() => Events.Count.ShouldEqual(0);
    [Fact] void should_not_invoke_any_callback_without_trusted_channel_metadata() => _unknownChannelCalls.ShouldEqual(0);
}
