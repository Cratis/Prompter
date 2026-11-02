// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_FeedbackProcessing.when_voting;

public class and_all_clicks_share_the_minute_budget : given.a_feedback_processor
{
    int _beforeRefill;

    void Establish()
    {
        Options.Protection.FeedbackPerMinute = 3;
        Create();
    }

    async Task Because()
    {
        await Run(1, 1);
        await Run(2, 1);
        await Processing.Run(3, "unrecognized", new(Acknowledge, Repeat, Refuse));
        for (ulong user = 4; user < 50; user++) await Run(user, (long)user);
        _beforeRefill = Acknowledgements;
        Clock.Advance(TimeSpan.FromMinutes(1));
        await Run(50, 50);
    }

    [Fact] void should_budget_malformed_acknowledgements() => _beforeRefill.ShouldEqual(2);
    [Fact] void should_budget_repeat_acknowledgements() => Repeats.ShouldEqual(1);
    [Fact] void should_refill_at_one_minute() => Acknowledgements.ShouldEqual(3);
    [Fact] async Task should_write_only_admitted_new_votes() => await Log.Received(2).RecordFeedback(Arg.Any<long>(), Arg.Any<FeedbackVerdict>(), Arg.Any<CancellationToken>());
    [Fact] void should_remain_silent_after_exhaustion() => Refusals.ShouldEqual(0);
}
