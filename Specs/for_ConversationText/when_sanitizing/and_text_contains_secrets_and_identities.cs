// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_ConversationText.when_sanitizing;

public class and_text_contains_secrets_and_identities : Specification
{
    string _result = null!;

    void Because() => _result = ConversationText.Sanitize(string.Concat(
        new string('x', 1000),
        " Bearer ",
        new string('a', 20),
        " <@123456789012345678> @example 123456789012345678 "));

    [Fact] void should_redact_the_secret() => _result.ShouldEqual(new string('x', 1000) + " " + SecretRedaction.Marker);
}
