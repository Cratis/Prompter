// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_Admission.when_checking;

public class and_the_channel_is_busy : given.an_admission
{
    AdmissionDecision _denied = null!;
    AdmissionDecision _other = null!;

    void Establish()
    {
        for (ulong user = 1; user <= 20; user++)
        {
            Guard.TryEnter(new(123, 1, user), "a question").Allowed.ShouldBeTrue();
        }
    }

    void Because()
    {
        _denied = Guard.TryEnter(new(123, 1, 21), "a question");
        _other = Guard.TryEnter(new(123, 2, 22), "a question");
    }

    [Fact] void should_stop_a_many_user_channel_flood() => _denied.Allowed.ShouldBeFalse();
    [Fact] void should_leave_other_channels_available() => _other.Allowed.ShouldBeTrue();
}
