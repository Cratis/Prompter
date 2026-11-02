// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_Admission.when_checking;

public class and_daily_requester_allowances_are_shared : given.an_admission
{
    readonly List<bool> _firstFive = [];
    AdmissionDecision _sixth = null!;
    AdmissionDecision _other = null!;
    AdmissionDecision _expired = null!;

    void Establish()
    {
        Options.Protection.UserAdmissionsPerDay = 5;
        Options.Protection.UserAdmissionCooldownSeconds = 60;
        Options.Protection.AllowedGuilds = [123, 124];
    }

    void Because()
    {
        for (ulong channel = 1; channel <= 5; channel++)
        {
            _firstFive.Add(Guard.TryEnter(new(channel % 2 == 0 ? 123UL : 124UL, channel, 1), "a question").Allowed);
            Clock.Advance(TimeSpan.FromMinutes(11));
        }
        _sixth = Guard.TryEnter(new(123, 6, 1), "a question");
        _other = Guard.TryEnter(new(123, 7, 2), "a question");
        Clock.Advance(TimeSpan.FromHours(24) - TimeSpan.FromMinutes(55));
        _expired = Guard.TryEnter(new(123, 8, 1), "a question");
    }

    [Fact] void should_allow_five_spaced_admissions() => _firstFive.TrueForAll(value => value).ShouldBeTrue();
    [Fact] void should_share_the_daily_limit_across_guilds_and_channels() => _sixth.Allowed.ShouldBeFalse();
    [Fact] void should_leave_another_requester_allowance_untouched() => _other.Allowed.ShouldBeTrue();
    [Fact] void should_expire_twenty_four_hours_after_the_first_admission() => _expired.Allowed.ShouldBeTrue();
}
