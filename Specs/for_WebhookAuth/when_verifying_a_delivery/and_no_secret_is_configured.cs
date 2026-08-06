// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_WebhookAuth.when_verifying_a_delivery;

public class and_no_secret_is_configured : Specification
{
    bool _authentic;

    void Because() => _authentic = WebhookAuth.IsAuthentic("sha256=abc", Encoding.UTF8.GetBytes("body"), string.Empty);

    [Fact] void should_refuse_every_caller() => _authentic.ShouldBeFalse();
}
