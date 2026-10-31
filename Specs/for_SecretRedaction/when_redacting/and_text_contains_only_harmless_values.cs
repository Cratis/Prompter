// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_SecretRedaction.when_redacting;

public class and_text_contains_only_harmless_values : Specification
{
    readonly string _text = "75f7f181-1111-4444-8888-123456789abc " + new string('a', 40) + " " + new string('b', 64) +
        " token: string CancellationToken token password secret 123456789012345678 https://cratis.io/docs";
    RedactedText _result = null!;

    void Because() => _result = SecretRedaction.Redact(_text);

    [Fact] void should_preserve_the_text() => _result.Text.ShouldEqual(_text);
    [Fact] void should_not_report_removal() => _result.Removed.ShouldBeFalse();
}
