// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;
using Pulumi;
using Pulumi.Utilities;

namespace Cratis.Prompter.Specs.for_ConfigurationFingerprint.when_fingerprinting;

public class and_the_values_are_secret : Specification
{
    Output<string> _output = null!;
    string _digest = null!;
    bool _secret;

    async Task Because()
    {
        _output = ConfigurationFingerprint.Create(new InputMap<string>
        {
            ["synthetic-key"] = Output.CreateSecret("synthetic-value"),
        });
        _digest = await OutputUtilities.GetValueAsync(_output);
        _secret = await Output.IsSecretAsync(_output);
    }

    [Fact] void should_retain_secret_taint() => _secret.ShouldBeTrue();
    [Fact] void should_contain_only_a_sha256_hex_digest() => (_digest.Length == 64 && _digest.All(char.IsAsciiHexDigit)).ShouldBeTrue();
    [Fact] void should_not_expose_the_value_in_the_annotation() => _digest.Contains("synthetic-value", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_key_in_the_annotation() => _digest.Contains("synthetic-key", StringComparison.Ordinal).ShouldBeFalse();
}
