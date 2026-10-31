// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Ask.when_rejecting;

public class and_the_thread_parent_is_denied : for_AskPrompter.given.an_interaction
{
    void Establish()
    {
        Options.Protection.DeniedChannels = [7];
        Select(thread: true);
    }

    async Task Because() => await Slash.Handle("How do I handle commands?");

    [Fact] void should_not_start_paid_work_or_read_history() => NoPaidWork();
    [Fact] void should_refuse_silently() => Payloads.Count.ShouldEqual(0);
    [Fact] void should_not_consume_admission() => Admission.TryEnter(new(1, 70, 3), "How do I handle commands?").Allowed.ShouldBeTrue();
}
