// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_FeedbackVerdicts.when_parsing;

public class and_the_token_is_up : Specification
{
    bool _parsed;
    FeedbackVerdict _verdict;

    void Because() => _parsed = FeedbackVerdicts.TryParse("up", out _verdict);

    [Fact] void should_parse() => _parsed.ShouldBeTrue();
    [Fact] void should_be_the_up_verdict() => _verdict.ShouldEqual(FeedbackVerdict.Up);
}
