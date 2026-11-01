// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_SecretRedaction.when_redacting;

public class and_redaction_times_out : Specification
{
    RedactedText _result = null!;

    void Because() => _result = SecretRedaction.Redact("untrusted text", _ => throw new RegexMatchTimeoutException());

    [Fact] void should_replace_the_entire_input() => _result.Text.ShouldEqual(SecretRedaction.Marker);
    [Fact] void should_report_removal() => _result.Removed.ShouldBeTrue();
}
