// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_FollowUps.when_filtering;

public class and_a_suggestion_contains_a_link : Specification
{
    [Fact] void should_apply_the_rule() => FollowUps.From(["See https://x.test"], "What is Arc?").ShouldBeEmpty();
}
