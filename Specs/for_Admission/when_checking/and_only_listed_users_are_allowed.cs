// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Admission.when_checking;

public class and_only_listed_users_are_allowed : given.an_admission
{
    bool _listed;
    bool _unlisted;
    bool _emptyList;

    void Establish() => Options.Protection.AllowedUsers = [7];

    void Because()
    {
        _listed = Guard.TryEnter(new(123, 1, 7), "question").Allowed;
        _unlisted = Guard.TryEnter(new(123, 1, 8), "question").Allowed;
        Options.Protection.AllowedUsers = [];
        _emptyList = Guard.TryEnter(new(123, 1, 9), "question").Allowed;
    }

    [Fact] void should_admit_a_listed_user() => _listed.ShouldBeTrue();
    [Fact] void should_refuse_an_unlisted_user() => _unlisted.ShouldBeFalse();
    [Fact] void should_admit_everyone_when_the_list_is_empty() => _emptyList.ShouldBeTrue();
}
