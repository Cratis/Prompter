// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_FeedbackVerdicts.when_parsing;

public class and_the_token_is_down : Specification
{
    bool _parsed;
    FeedbackVerdict _verdict;

    void Because() => _parsed = FeedbackVerdicts.TryParse("down", out _verdict);

    [Fact] void should_parse() => _parsed.ShouldBeTrue();
    [Fact] void should_be_the_down_verdict() => _verdict.ShouldEqual(FeedbackVerdict.Down);
}
