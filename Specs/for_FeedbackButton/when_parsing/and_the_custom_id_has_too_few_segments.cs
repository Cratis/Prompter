// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_FeedbackButton.when_parsing;

public class and_the_custom_id_has_too_few_segments : Specification
{
    bool _rejected;

    void Because() => _rejected = FeedbackButton.Parse("feedback:up") is null;

    [Fact] void should_reject_the_custom_id() => _rejected.ShouldBeTrue();
}
