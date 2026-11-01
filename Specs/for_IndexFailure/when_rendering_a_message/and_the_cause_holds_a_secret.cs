// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Cli;
using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_IndexFailure.when_rendering_a_message;

public class and_the_cause_holds_a_secret : Specification
{
    string _message = null!;

    void Because() => _message = IndexFailure.Message(new InvalidOperationException("Connection refused for api_key=placeholder-value"));

    [Fact] void should_redact_the_value() => _message.ShouldNotContain("placeholder-value");
    [Fact] void should_mark_the_redaction() => _message.ShouldContain(SecretRedaction.Marker);
}
