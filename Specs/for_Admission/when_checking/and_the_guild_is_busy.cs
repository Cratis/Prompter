// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_Admission.when_checking;

public class and_the_guild_is_busy : given.an_admission
{
    AdmissionDecision _denied = null!;
    AdmissionDecision _expired = null!;

    void Establish()
    {
        for (ulong user = 1; user <= 60; user++)
        {
            Guard.TryEnter(new(123, user, user), "a question").Allowed.ShouldBeTrue();
        }
    }

    void Because()
    {
        _denied = Guard.TryEnter(new(123, 999, 999), "a question");
        Clock.Advance(TimeSpan.FromHours(1));
        _expired = Guard.TryEnter(new(123, 999, 999), "a question");
    }

    [Fact] void should_stop_a_cross_channel_flood() => _denied.Allowed.ShouldBeFalse();
    [Fact] void should_expire_the_window() => _expired.Allowed.ShouldBeTrue();
}
