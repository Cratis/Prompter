// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_GuidanceButtons.when_rejecting;

public class and_the_request_is_not_admitted : for_AskPrompter.given.an_interaction
{
    [Theory]
    [InlineData(null, false, 7UL)]
    [InlineData(2UL, false, 7UL)]
    [InlineData(1UL, true, 7UL)]
    [InlineData(1UL, false, 70UL)]
    public async Task should_not_read_history_or_start_paid_work(ulong? guild, bool thread, ulong denied)
    {
        Options.Protection.DeniedChannels = [denied];
        Button("Which product?\n\n> How do I handle commands?", guild, thread, "clarify:arc");
        await Buttons.Clarify();
        NoPaidWork();
    }
}
