// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_ProtectionOptions.when_validating;

public class and_feedback_work_limits_are_configured : Specification
{
    bool _invalid;
    bool _bounds;

    void Because()
    {
        ProtectionOptions[] invalid =
        [
            new() { FeedbackConcurrency = 0 }, new() { FeedbackConcurrency = 9 },
            new() { FeedbackPerMinute = -1 }, new() { FeedbackPerMinute = 121 },
            new() { FeedbackWriteTimeoutMilliseconds = 99 }, new() { FeedbackWriteTimeoutMilliseconds = 2501 }
        ];
        _invalid = invalid.All(options => !options.IsValid);
        _bounds = new ProtectionOptions { FeedbackConcurrency = 8, FeedbackPerMinute = 0, FeedbackWriteTimeoutMilliseconds = 100 }.IsValid &&
            new ProtectionOptions { FeedbackPerMinute = 120, FeedbackWriteTimeoutMilliseconds = 2500 }.IsValid;
    }

    [Fact] void should_reject_invalid_bounds() => _invalid.ShouldBeTrue();
    [Fact] void should_accept_the_documented_boundaries() => _bounds.ShouldBeTrue();
    [Fact] void should_default_to_two_concurrent_lifecycles() => new ProtectionOptions().FeedbackConcurrency.ShouldEqual(2);
    [Fact] void should_default_to_thirty_clicks_per_minute() => new ProtectionOptions().FeedbackPerMinute.ShouldEqual(30);
    [Fact] void should_default_to_a_two_second_deadline() => new ProtectionOptions().FeedbackWriteTimeoutMilliseconds.ShouldEqual(2000);
}
