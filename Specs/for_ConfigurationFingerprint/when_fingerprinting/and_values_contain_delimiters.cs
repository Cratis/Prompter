// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;
using Pulumi;
using Pulumi.Utilities;

namespace Cratis.Prompter.Specs.for_ConfigurationFingerprint.when_fingerprinting;

public class and_values_contain_delimiters : Specification
{
    string _first = null!;
    string _second = null!;

    async Task Because()
    {
        _first = await OutputUtilities.GetValueAsync(ConfigurationFingerprint.Create(new InputMap<string> { ["a"] = "one\nb=two" }));
        _second = await OutputUtilities.GetValueAsync(ConfigurationFingerprint.Create(new InputMap<string> { ["a"] = "one", ["b"] = "two" }));
    }

    [Fact] void should_keep_distinct_maps_distinct() => (_first != _second).ShouldBeTrue();
}
