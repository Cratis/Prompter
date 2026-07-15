// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_FeedbackVerdicts.when_parsing;

public class and_the_token_is_unknown : Specification
{
    bool _parsed;

    void Because() => _parsed = FeedbackVerdicts.TryParse("sideways", out _);

    [Fact] void should_not_parse() => _parsed.ShouldBeFalse();
}
