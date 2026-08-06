// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueDraftPrompt.when_building_the_user_message;

public class and_the_reporter_added_a_hint : Specification
{
    string _message = null!;

    void Because() => _message = IssueDraftPrompt.UserMessage("It crashes on startup.", "I think it's the migration");

    [Fact] void should_include_the_conversation() => _message.ShouldContain("It crashes on startup.");
    [Fact] void should_include_the_hint() => _message.ShouldContain("I think it's the migration");
}
