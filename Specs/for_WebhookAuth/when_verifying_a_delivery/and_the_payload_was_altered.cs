// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_WebhookAuth.when_verifying_a_delivery;

public class and_the_payload_was_altered : Specification
{
    const string Secret = "s3cret";
    bool _authentic;

    void Because()
    {
        var signed = Encoding.UTF8.GetBytes("the original body");
        var signature = "sha256=" + Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), signed)).ToLower(CultureInfo.InvariantCulture);

        _authentic = WebhookAuth.IsAuthentic(signature, Encoding.UTF8.GetBytes("a tampered body"), Secret);
    }

    [Fact] void should_refuse_the_delivery() => _authentic.ShouldBeFalse();
}
