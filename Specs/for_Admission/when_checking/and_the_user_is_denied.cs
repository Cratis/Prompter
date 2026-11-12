// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Admission.when_checking;

public class and_the_user_is_denied : given.an_admission
{
    Protection.AdmissionDecision _denied = null!;
    Protection.AdmissionDecision _maintainer = null!;
    Protection.AdmissionDecision _other = null!;
    bool _permitsDenied;

    void Establish() => Options.Protection.DeniedUsers = [7];

    void Because()
    {
        _denied = Guard.TryEnter(new(123, 1, 7), "question");
        _maintainer = Guard.TryEnter(new(123, 1, 7, Maintainer: true), "question");
        _other = Guard.TryEnter(new(123, 1, 8), "question");
        _permitsDenied = Guard.Permits(7);
    }

    [Fact] void should_refuse_silently() => (_denied.Allowed || _denied.Reply || _denied.Text is not null).ShouldBeFalse();
    [Fact] void should_not_exempt_a_maintainer() => _maintainer.Allowed.ShouldBeFalse();
    [Fact] void should_admit_other_users() => _other.Allowed.ShouldBeTrue();
    [Fact] void should_not_permit_the_user() => _permitsDenied.ShouldBeFalse();
}
