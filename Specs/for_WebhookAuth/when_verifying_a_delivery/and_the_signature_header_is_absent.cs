// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_WebhookAuth.when_verifying_a_delivery;

public class and_the_signature_header_is_absent : Specification
{
    bool _authentic;

    void Because() => _authentic = WebhookAuth.IsAuthentic(null, Encoding.UTF8.GetBytes("body"), "s3cret");

    [Fact] void should_refuse_the_delivery() => _authentic.ShouldBeFalse();
}
