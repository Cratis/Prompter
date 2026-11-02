// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Admission.when_checking;

public class and_nonadmitted_work_does_not_spend_requester_allowances : given.an_admission
{
    bool _allowed;
    bool _afterDuplicate;

    void Establish() => Options.Protection.UserAdmissionsPerDay = 2;

    void Because()
    {
        Guard.TryEnter(new(null, 1, 1), "question");
        Guard.TryEnter(new(123, 1, 1), " ");
        Guard.MayReply(new(123, 1, 1));
        _allowed = Guard.TryEnter(new(123, 1, 1), "question").Allowed;
        Clock.Advance(TimeSpan.FromSeconds(60));
        Guard.TryEnter(new(123, 1, 1), "  QUESTION  ").Allowed.ShouldBeFalse();
        _afterDuplicate = Guard.TryEnter(new(123, 2, 1), "new question").Allowed;
    }

    [Fact] void should_not_charge_malformed_out_of_scope_or_refusal_work() => _allowed.ShouldBeTrue();
    [Fact] void should_not_charge_a_duplicate_or_extend_the_cooldown() => _afterDuplicate.ShouldBeTrue();
}
