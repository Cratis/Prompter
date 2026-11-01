// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_ConversationText.when_sanitizing;

public class and_sanitization_times_out : Specification
{
    string _result = null!;

    void Because() => _result = ConversationText.Sanitize("untrusted text", _ => throw new RegexMatchTimeoutException());

    [Fact] void should_replace_the_entire_input() => _result.ShouldEqual(SecretRedaction.Marker);
}
