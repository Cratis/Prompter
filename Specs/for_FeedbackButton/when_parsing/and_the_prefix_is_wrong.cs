// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_FeedbackButton.when_parsing;

public class and_the_prefix_is_wrong : Specification
{
    bool _rejected;

    void Because() => _rejected = FeedbackButton.Parse("vote:up:12345") is null;

    [Fact] void should_reject_the_custom_id() => _rejected.ShouldBeTrue();
}
