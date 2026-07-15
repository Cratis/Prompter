// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_FeedbackVerdicts.when_producing_text;

public class and_the_verdict_is_down : Specification
{
    string _text = null!;

    void Because() => _text = FeedbackVerdict.Down.ToText();

    [Fact] void should_be_the_down_token() => _text.ShouldEqual("down");
}
