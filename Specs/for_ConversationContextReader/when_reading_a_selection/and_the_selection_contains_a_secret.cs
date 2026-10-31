// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_a_selection;

public class and_the_selection_contains_a_secret : given.a_selection_reader
{
    string _secret = string.Empty;
    void Establish()
    {
        in_thread = false;
        _secret = $"ghp_{new string('a', 32)}";
    }
    async Task Because() => result = await Select($"Please explain this {_secret}");
    [Fact] void should_remove_the_secret_before_model_input() => result.Value.Contains(_secret, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_keep_the_question() => result.Value.Contains("Please explain this", StringComparison.Ordinal).ShouldBeTrue();
}
