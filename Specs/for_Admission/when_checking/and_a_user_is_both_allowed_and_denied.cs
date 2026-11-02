// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Admission.when_checking;

public class and_a_user_is_both_allowed_and_denied : given.an_admission
{
    bool _permitted;

    void Establish()
    {
        Options.Protection.AllowedUsers = [7];
        Options.Protection.DeniedUsers = [7];
    }

    void Because() => _permitted = Guard.Permits(7);

    [Fact] void should_let_denial_win() => _permitted.ShouldBeFalse();
}
