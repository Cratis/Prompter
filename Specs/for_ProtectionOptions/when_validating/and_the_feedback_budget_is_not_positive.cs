// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_ProtectionOptions.when_validating;

public class and_the_feedback_budget_is_not_positive : Specification
{
    bool _defaults;
    bool _noClicks;
    bool _noWindow;

    void Because()
    {
        _defaults = new ProtectionOptions().IsValid;
        _noClicks = new ProtectionOptions { FeedbackClicksPerUser = 0 }.IsValid;
        _noWindow = new ProtectionOptions { FeedbackWindowSeconds = 0 }.IsValid;
    }

    [Fact] void should_accept_the_defaults() => _defaults.ShouldBeTrue();
    [Fact] void should_reject_zero_clicks() => _noClicks.ShouldBeFalse();
    [Fact] void should_reject_a_zero_window() => _noWindow.ShouldBeFalse();
}
