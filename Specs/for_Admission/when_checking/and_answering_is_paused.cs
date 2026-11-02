// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_Admission.when_checking;

public class and_answering_is_paused : given.an_admission
{
    AdmissionDecision _paused = null!;
    AdmissionDecision _maintainer = null!;
    bool _inScope;
    AdmissionDecision _resumed = null!;

    void Because()
    {
        Answering.BeginPause();
        _paused = Guard.TryEnter(new(123, 1, 1), "question");
        _maintainer = Guard.TryEnter(new(123, 1, 2, Maintainer: true), "question");
        _inScope = Guard.InScope(123, 1);
        Answering.Resumed();
        _resumed = Guard.TryEnter(new(123, 1, 1), "question");
    }

    [Fact] void should_refuse_silently() => (_paused.Allowed || _paused.Reply).ShouldBeFalse();
    [Fact] void should_refuse_maintainers_too() => _maintainer.Allowed.ShouldBeFalse();
    [Fact] void should_be_out_of_scope_like_a_disabled_bot() => _inScope.ShouldBeFalse();
    [Fact] void should_admit_again_after_resuming() => _resumed.Allowed.ShouldBeTrue();
}
