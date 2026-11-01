// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_ConversationText.when_sanitizing;

public class and_text_is_long_and_harmless : Specification
{
    [Theory]
    [InlineData('x', 1000)]
    [InlineData('q', 4000)]
    void should_preserve_the_text(char character, int length)
    {
        var text = new string(character, length);
        ConversationText.Sanitize(text).ShouldEqual(text);
    }
}
