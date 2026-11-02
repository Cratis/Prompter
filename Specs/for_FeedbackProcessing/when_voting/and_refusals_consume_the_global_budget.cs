// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_FeedbackProcessing.when_voting;

public class and_refusals_consume_the_global_budget : given.a_feedback_processor
{
    void Establish()
    {
        Options.Protection.FeedbackClicksPerUser = 1;
        Options.Protection.FeedbackPerMinute = 2;
        Create();
    }

    async Task Because()
    {
        for (var answer = 1; answer < 50; answer++) await Run(1, answer);
    }

    [Fact] void should_not_send_unbudgeted_refusals() => Refusals.ShouldEqual(1);
    [Fact] void should_not_acknowledge_denied_clicks() => Acknowledgements.ShouldEqual(1);
}
