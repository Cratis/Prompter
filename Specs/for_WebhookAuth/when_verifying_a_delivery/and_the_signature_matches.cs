// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_WebhookAuth.when_verifying_a_delivery;

public class and_the_signature_matches : Specification
{
    const string Secret = "s3cret";
    readonly byte[] _payload = Encoding.UTF8.GetBytes("""{"action":"opened"}""");
    bool _authentic;

    void Because()
    {
        var signature = "sha256=" + Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), _payload)).ToLower(CultureInfo.InvariantCulture);

        _authentic = WebhookAuth.IsAuthentic(signature, _payload, Secret);
    }

    [Fact] void should_accept_the_delivery() => _authentic.ShouldBeTrue();
}
