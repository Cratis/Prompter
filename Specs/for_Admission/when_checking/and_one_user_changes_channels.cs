// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_Admission.when_checking;

public class and_one_user_changes_channels : given.an_admission
{
    AdmissionDecision _result = null!;

    void Establish()
    {
        for (ulong channel = 1; channel <= 5; channel++)
        {
            Guard.TryEnter(new(123, channel, 1), "a question").Allowed.ShouldBeTrue();
        }
    }

    void Because() => _result = Guard.TryEnter(new(123, 6, 1), "a question");

    [Fact] void should_share_the_per_user_limit_across_channels() => _result.Allowed.ShouldBeFalse();
}
