// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_Admission.when_refusing;

public class and_the_channel_is_busy_without_striking : given.an_admission
{
    AdmissionDecision _busy = null!;
    AdmissionDecision _recovered = null!;

    void Establish()
    {
        Options.Protection.GlobalPerHour = 1;
        Options.Protection.UserRefusalSeconds = 1;
        Options.Protection.ChannelRefusalSeconds = 1;
    }

    void Because()
    {
        Guard.TryEnter(new(123, 1, 1), "first");
        for (var i = 0; i < 6; i++)
        {
            Clock.Advance(TimeSpan.FromSeconds(2));
            _busy = Guard.TryEnter(new(123, 1, 2), $"q{i}");
        }

        Options.Protection.GlobalPerHour = 100;
        _recovered = Guard.TryEnter(new(123, 1, 2), "after");
    }

    [Fact] void should_report_the_busy_limit() => _busy.Text.ShouldContain("answering a lot of questions");
    [Fact] void should_not_pause_the_requester() => _recovered.Allowed.ShouldBeTrue();
}
