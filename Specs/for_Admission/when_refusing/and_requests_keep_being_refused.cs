// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_Admission.when_refusing;

public class and_requests_keep_being_refused : given.an_admission
{
    AdmissionDecision _paused = null!;
    AdmissionDecision _maintainer = null!;
    AdmissionDecision _afterPause = null!;

    void Establish()
    {
        Options.Protection.UserAdmissionCooldownSeconds = 86400;
        Options.Protection.UserRefusalSeconds = 1;
        Options.Protection.ChannelRefusalSeconds = 1;
    }

    void Because()
    {
        Guard.TryEnter(new(123, 1, 1), "q0");
        for (var i = 1; i <= 5; i++)
        {
            Clock.Advance(TimeSpan.FromSeconds(2));
            Guard.TryEnter(new(123, 1, 1), $"q{i}");
        }

        Clock.Advance(TimeSpan.FromSeconds(2));
        _paused = Guard.TryEnter(new(123, 1, 1), "q6");
        _maintainer = Guard.TryEnter(new(123, 1, 1, Maintainer: true), "maintainer");
        Clock.Advance(TimeSpan.FromSeconds(300));
        _afterPause = Guard.TryEnter(new(123, 1, 1), "q7");
    }

    [Fact] void should_pause_the_requester_and_name_the_wait() => _paused.Text.ShouldContain("pausing your questions for about 5 minutes");
    [Fact] void should_not_pause_a_maintainer() => _maintainer.Allowed.ShouldBeTrue();
    [Fact] void should_end_the_pause_without_extending_it() => _afterPause.Text.ShouldContain("Give me a moment");
}
