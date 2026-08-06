// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueDraftPrompt.when_building_the_user_message;

public class and_there_is_no_hint : Specification
{
    string _message = null!;

    void Because() => _message = IssueDraftPrompt.UserMessage("It crashes on startup.", null);

    [Fact] void should_include_the_conversation() => _message.ShouldContain("It crashes on startup.");
    [Fact] void should_not_mention_a_reporter_addition() => _message.Contains("The reporter adds", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_ask_for_json() => _message.ShouldContain("as JSON");
}
