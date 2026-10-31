// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetCord;

namespace Cratis.Prompter.Specs.for_AskPrompter.when_rejecting;

public class and_the_selection_is_not_admitted : given.an_interaction
{
    [Theory]
    [InlineData(null, false, false, MessageType.Default)]
    [InlineData(2UL, false, false, MessageType.Default)]
    [InlineData(1UL, true, false, MessageType.Default)]
    [InlineData(1UL, false, true, MessageType.Default)]
    [InlineData(1UL, false, false, MessageType.GuildMembedJoined)]
    public async Task should_reject_without_history_or_paid_work(ulong? guild, bool thread, bool bot, MessageType type)
    {
        Options.Protection.DeniedChannels = [7];
        Select(guild, thread, bot, type);
        await Command.Handle();
        NoPaidWork();
    }

    [Fact]
    public async Task should_reject_admission_denial_before_reading_history()
    {
        Options.Protection.DeniedChannels = [70];
        Select();
        await Command.Handle();
        NoPaidWork();
    }
}
